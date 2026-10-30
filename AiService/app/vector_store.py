import chromadb
from chromadb.utils import embedding_functions

from app.config import settings
from app.chunking import Chunk

_client = chromadb.PersistentClient(path=settings.chroma_persist_dir)

_embedding_function = embedding_functions.SentenceTransformerEmbeddingFunction(
    model_name=settings.embedding_model_name
)

_collection = _client.get_or_create_collection(
    name=settings.chroma_collection_name,
    embedding_function=_embedding_function,
)


def index_document(
    document_id: int,
    title: str,
    file_name: str,
    chunks: list[Chunk],
) -> int:
    """
    Indexe les chunks d'un document. Si le document avait déjà été indexé
    (ré-upload / ré-indexation manuelle), on supprime d'abord ses anciens
    chunks pour éviter les doublons.
    """
    delete_document(document_id)

    if not chunks:
        return 0

    ids = [f"{document_id}-{i}" for i in range(len(chunks))]
    documents = [c.text for c in chunks]
    metadatas = [
        {
            "document_id": document_id,
            "title": title,
            "file_name": file_name,
            "page_number": c.page_number,
        }
        for c in chunks
    ]

    _collection.add(ids=ids, documents=documents, metadatas=metadatas)

    return len(chunks)


def delete_document(document_id: int) -> None:
    _collection.delete(where={"document_id": document_id})


def search(query: str, top_k: int, max_distance: float | None = None) -> list[dict]:
    """
    Récupère les chunks les plus proches de la question, puis exclut ceux
    dont la distance dépasse `max_distance` (chunk pas vraiment pertinent).

    Sans ce filtre, ChromaDB renvoie toujours les `top_k` résultats les
    "moins mauvais", même si aucun document ne traite réellement du sujet
    de la question — ce qui produisait des sources hors-sujet.
    """
    if max_distance is None:
        max_distance = settings.retrieval_max_distance

    results = _collection.query(
        query_texts=[query],
        n_results=top_k,
        include=["documents", "metadatas", "distances"],
    )

    if not results["documents"] or not results["documents"][0]:
        return []

    hits = []
    for text, metadata, distance in zip(
        results["documents"][0],
        results["metadatas"][0],
        results["distances"][0],
    ):
        if distance > max_distance:
            continue
        hits.append({"text": text, "distance": distance, **metadata})

    return hits