import json
from typing import Optional

import httpx
from fastapi import Depends, FastAPI, UploadFile, File, Form, HTTPException
from fastapi.responses import StreamingResponse
from pydantic import BaseModel

from app import history_store
from app.auth import get_current_user_id
from app.pdf_extractor import extract_pages
from app.chunking import chunk_pages
from app.vector_store import index_document, delete_document, search
from app.llm_client import stream_answer
from app.config import settings

app = FastAPI(
    title="CVT AI Service",
    description="Extraction, indexation et question-réponse (RAG) sur les documents du centre.",
)


@app.get("/health")
async def health():
    return {"status": "ok"}


# ============================================================
# INGESTION — appelé automatiquement par DocumentService après
# chaque upload réussi, ou manuellement pour ré-indexer un document
# existant.
# ============================================================

@app.post("/ingest")
async def ingest(
    document_id: int = Form(...),
    title: str = Form(...),
    file_name: str = Form(...),
    file: Optional[UploadFile] = File(None),
):
    if file is None:
        raise HTTPException(status_code=400, detail="Le fichier est manquant.")

    file_bytes = await file.read()

    if not file_bytes:
        raise HTTPException(status_code=400, detail="Le fichier est vide.")

    if not file_name.lower().endswith(".pdf"):
        # Prototype : on ne traite que les PDF, cohérent avec le cahier
        # des charges (procédures, notes de service, etc. sont des PDF).
        return {
            "document_id": document_id,
            "indexed_chunks": 0,
            "message": "Fichier ignoré : seuls les PDF sont indexés pour l'instant.",
        }

    pages_text = extract_pages(file_bytes)
    chunks = chunk_pages(pages_text)

    indexed_count = index_document(
        document_id=document_id,
        title=title,
        file_name=file_name,
        chunks=chunks,
    )

    return {
        "document_id": document_id,
        "indexed_chunks": indexed_count,
    }


@app.delete("/ingest/{document_id}")
async def remove_from_index(document_id: int):
    """Appelé quand un document est supprimé côté DocumentService."""
    delete_document(document_id)
    return {"document_id": document_id, "message": "Document retiré de l'index."}


# ============================================================
# HISTORIQUE DES CONVERSATIONS
# ============================================================

@app.get("/conversations")
async def get_conversations(user_id: str = Depends(get_current_user_id)):
    return history_store.list_conversations(user_id)


@app.get("/conversations/{conversation_id}")
async def get_conversation(
    conversation_id: int,
    user_id: str = Depends(get_current_user_id),
):
    owner = history_store.get_conversation_owner(conversation_id)
    if owner is None or owner != user_id:
        raise HTTPException(status_code=404, detail="Conversation introuvable.")

    return {
        "id": conversation_id,
        "messages": history_store.get_messages(conversation_id),
    }


@app.delete("/conversations/{conversation_id}")
async def remove_conversation(
    conversation_id: int,
    user_id: str = Depends(get_current_user_id),
):
    owner = history_store.get_conversation_owner(conversation_id)
    if owner is None or owner != user_id:
        raise HTTPException(status_code=404, detail="Conversation introuvable.")

    history_store.delete_conversation(conversation_id)
    return {"message": "Conversation supprimée."}


# ============================================================
# QUESTION / RÉPONSE (RAG) — en streaming (SSE)
# ============================================================

class AskRequest(BaseModel):
    question: str
    conversation_id: Optional[int] = None


def _dedupe_sources(hits: list[dict]) -> list[dict]:
    """Plusieurs chunks retrouvés peuvent venir du même document/page :
    on ne montre chaque (fichier, page) qu'une seule fois, dans l'ordre
    de pertinence."""
    seen: dict[tuple[str, int], dict] = {}
    for hit in hits:
        key = (hit["file_name"], hit["page_number"])
        if key not in seen:
            seen[key] = {
                "file_name": hit["file_name"],
                "title": hit["title"],
                "page_number": hit["page_number"],
            }
    return list(seen.values())


def _sse(data: dict) -> str:
    return f"data: {json.dumps(data, ensure_ascii=False)}\n\n"


NO_CONTEXT_ANSWER = (
    "Je ne trouve aucune information pertinente dans les documents "
    "disponibles pour répondre à cette question."
)


@app.post("/ask")
async def ask(
    request: AskRequest,
    user_id: str = Depends(get_current_user_id),
):
    question = request.question.strip() if request.question else ""
    if not question:
        raise HTTPException(status_code=400, detail="La question est obligatoire.")

    conversation_id = request.conversation_id

    if conversation_id is not None:
        owner = history_store.get_conversation_owner(conversation_id)
        if owner is None or owner != user_id:
            raise HTTPException(status_code=404, detail="Conversation introuvable.")
        history_for_llm = [
            {"role": m["role"], "content": m["content"]}
            for m in history_store.get_messages(conversation_id)
        ]
    else:
        conversation_id = history_store.create_conversation(user_id, question)
        history_for_llm = []

    hits = search(question, top_k=settings.retrieval_top_k)
    sources = _dedupe_sources(hits)

    async def event_stream():
        yield _sse({"type": "start", "conversation_id": conversation_id})

        answer_parts: list[str] = []

        if not hits:
            answer_parts.append(NO_CONTEXT_ANSWER)
            yield _sse({"type": "token", "content": NO_CONTEXT_ANSWER})
        else:
            try:
                async for token in stream_answer(question, hits, history_for_llm):
                    answer_parts.append(token)
                    yield _sse({"type": "token", "content": token})
            except (httpx.ConnectError, httpx.TimeoutException):
                yield _sse(
                    {
                        "type": "error",
                        "detail": (
                            "Le modèle IA (Ollama) est indisponible pour le "
                            "moment. Vérifiez qu'il est démarré et que le "
                            "modèle a bien été téléchargé (voir README)."
                        ),
                    }
                )
                return
            except httpx.HTTPStatusError as exc:
                yield _sse(
                    {
                        "type": "error",
                        "detail": f"Erreur retournée par le modèle IA : {exc.response.status_code}.",
                    }
                )
                return

        full_answer = "".join(answer_parts)

        history_store.add_message(conversation_id, "user", question)
        history_store.add_message(conversation_id, "assistant", full_answer, sources)
        history_store.touch_conversation(conversation_id)

        yield _sse({"type": "done", "conversation_id": conversation_id, "sources": sources})

    return StreamingResponse(
        event_stream(),
        media_type="text/event-stream",
        headers={
            # Évite qu'un proxy intermédiaire (nginx, etc.) mette le flux
            # en tampon et casse l'effet "mot par mot".
            "Cache-Control": "no-cache",
            "X-Accel-Buffering": "no",
        },
    )
