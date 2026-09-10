import json
from typing import AsyncIterator

import httpx
from app.config import settings

SYSTEM_PROMPT = (
    "Tu es l'assistant documentaire du Centre de Contrôle Automobile et "
    "Expertise Oujda (réseau DEKRA Maroc). Réponds UNIQUEMENT à partir du "
    "contexte fourni ci-dessous, extrait des documents internes du centre. "
    "Si le contexte ne contient pas l'information demandée, dis clairement "
    "que tu ne trouves pas cette information dans les documents disponibles, "
    "sans inventer de réponse. Réponds en français, en 3 à 5 phrases maximum "
    "sauf si la question demande explicitement une liste ou une procédure "
    "détaillée étape par étape. Va droit au but, sans reformuler la question."
)

OLLAMA_OPTIONS = {
    # Coupe la génération après ~350 tokens : évite les réponses qui
    # partent en digression et font attendre l'utilisateur pour rien
    # (le modèle tourne en CPU, chaque token coûte cher).
    "num_predict": 350,
    # Température basse : réponses plus directes et déterministes, moins
    # de "remplissage" verbeux autour de l'information utile.
    "temperature": 0.3,
}


def _build_messages(
    question: str,
    context_chunks: list[dict],
    history: list[dict] | None = None,
) -> list[dict]:
    """
    Construit la liste de messages envoyée à Ollama : prompt système, puis
    l'historique des échanges précédents de la conversation (pour la
    continuité multi-tours), puis la question courante enrichie du
    contexte documentaire retrouvé par le RAG.
    """
    context_text = (
        "\n\n---\n\n".join(
            f"[Source : {c['file_name']}, page {c['page_number']}]\n{c['text']}"
            for c in context_chunks
        )
        if context_chunks
        else "(aucun extrait de document suffisamment pertinent n'a été trouvé)"
    )

    messages = [{"role": "system", "content": SYSTEM_PROMPT}]

    for turn in history or []:
        messages.append({"role": turn["role"], "content": turn["content"]})

    messages.append(
        {
            "role": "user",
            "content": (
                f"Contexte :\n{context_text}\n\n"
                f"Question : {question}\n\n"
                "Réponds en te basant uniquement sur le contexte ci-dessus."
            ),
        }
    )

    return messages


async def stream_answer(
    question: str,
    context_chunks: list[dict],
    history: list[dict] | None = None,
) -> AsyncIterator[str]:
    """
    Génère la réponse token par token en streaming depuis Ollama.

    Ollama renvoie une suite de lignes JSON (une par token/petit paquet de
    tokens), chacune avec `message.content` = le morceau de texte généré
    et `done` = false, jusqu'à une dernière ligne avec `done = true`.
    """
    payload = {
        "model": settings.ollama_model,
        "messages": _build_messages(question, context_chunks, history),
        "stream": True,
        "options": OLLAMA_OPTIONS,
    }

    async with httpx.AsyncClient(timeout=300.0) as client:
        async with client.stream(
            "POST",
            f"{settings.ollama_base_url}/api/chat",
            json=payload,
        ) as response:
            response.raise_for_status()

            async for line in response.aiter_lines():
                if not line.strip():
                    continue

                chunk = json.loads(line)
                content = chunk.get("message", {}).get("content", "")

                if content:
                    yield content

                if chunk.get("done"):
                    break