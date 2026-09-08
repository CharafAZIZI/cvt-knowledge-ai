import httpx
from app.config import settings

SYSTEM_PROMPT = (
    "Tu es l'assistant documentaire du Centre de Contrôle Automobile et "
    "Expertise Oujda (réseau DEKRA Maroc). Réponds UNIQUEMENT à partir du "
    "contexte fourni ci-dessous, extrait des documents internes du centre. "
    "Si le contexte ne contient pas l'information demandée, dis clairement "
    "que tu ne trouves pas cette information dans les documents disponibles, "
    "sans inventer de réponse. Réponds en français, de façon claire et "
    "concise."
)


async def generate_answer(question: str, context_chunks: list[dict]) -> str:
    context_text = "\n\n---\n\n".join(
        f"[Source : {c['file_name']}, page {c['page_number']}]\n{c['text']}"
        for c in context_chunks
    )

    prompt = (
        f"Contexte :\n{context_text}\n\n"
        f"Question : {question}\n\n"
        "Réponds en te basant uniquement sur le contexte ci-dessus."
    )

    payload = {
        "model": settings.ollama_model,
        "messages": [
            {"role": "system", "content": SYSTEM_PROMPT},
            {"role": "user", "content": prompt},
        ],
        "stream": False,
    }

    async with httpx.AsyncClient(timeout=300.0) as client:
        response = await client.post(
            f"{settings.ollama_base_url}/api/chat",
            json=payload,
        )
        response.raise_for_status()
        data = response.json()

    return data["message"]["content"].strip()
