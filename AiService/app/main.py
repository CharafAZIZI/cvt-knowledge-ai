from typing import Optional

import httpx
from fastapi import FastAPI, UploadFile, File, Form, HTTPException
from pydantic import BaseModel

from app.pdf_extractor import extract_pages
from app.chunking import chunk_pages
from app.vector_store import index_document, delete_document, search
from app.llm_client import generate_answer
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
# QUESTION / RÉPONSE (RAG)
# ============================================================

class AskRequest(BaseModel):
    question: str


class Source(BaseModel):
    file_name: str
    title: str
    page_number: int


class AskResponse(BaseModel):
    answer: str
    sources: list[Source]


@app.post("/ask", response_model=AskResponse)
async def ask(request: AskRequest):
    if not request.question or not request.question.strip():
        raise HTTPException(status_code=400, detail="La question est obligatoire.")

    hits = search(request.question, top_k=settings.retrieval_top_k)

    if not hits:
        return AskResponse(
            answer=(
                "Je ne trouve aucune information pertinente dans les "
                "documents disponibles pour répondre à cette question."
            ),
            sources=[],
        )

    try:
        answer = await generate_answer(request.question, hits)
    except (httpx.ConnectError, httpx.TimeoutException) as exc:
        # Ollama indisponible/pas encore démarré : erreur claire plutôt
        # qu'une 500 générique sans explication.
        raise HTTPException(
            status_code=503,
            detail=(
                "Le modèle IA (Ollama) est indisponible pour le moment. "
                "Vérifiez qu'il est démarré et que le modèle a bien été "
                "téléchargé (voir README)."
            ),
        ) from exc
    except httpx.HTTPStatusError as exc:
        raise HTTPException(
            status_code=502,
            detail=f"Erreur retournée par le modèle IA : {exc.response.status_code}.",
        ) from exc

    sources = [
        Source(
            file_name=h["file_name"],
            title=h["title"],
            page_number=h["page_number"],
        )
        for h in hits
    ]

    # Dédupliquer les sources (plusieurs chunks peuvent venir du même
    # document/page).
    unique_sources = list({(s.file_name, s.page_number): s for s in sources}.values())

    return AskResponse(answer=answer, sources=unique_sources)
