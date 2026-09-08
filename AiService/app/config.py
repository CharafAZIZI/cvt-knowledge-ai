from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    # Ollama (LLM local, pas de clé API)
    ollama_base_url: str = "http://ollama:11434"
    ollama_model: str = "llama3.2:3b"

    # ChromaDB (vector store local, persisté sur disque)
    chroma_persist_dir: str = "/data/chroma"
    chroma_collection_name: str = "cvt_documents"

    # Modèle d'embeddings local (sentence-transformers, CPU)
    embedding_model_name: str = "all-MiniLM-L6-v2"

    # Chunking
    chunk_size_chars: int = 1200
    chunk_overlap_chars: int = 150

    # Nombre de chunks récupérés pour répondre à une question
    retrieval_top_k: int = 4

    class Config:
        env_prefix = "AI_"


settings = Settings()

# Garde-fou : si overlap >= size, le chunking boucle indéfiniment
# (start ne progresse plus). On échoue au démarrage plutôt qu'en
# plein traitement d'un document.
if settings.chunk_overlap_chars >= settings.chunk_size_chars:
    raise ValueError(
        "AI_CHUNK_OVERLAP_CHARS doit être strictement inférieur à "
        "AI_CHUNK_SIZE_CHARS (actuellement "
        f"{settings.chunk_overlap_chars} >= {settings.chunk_size_chars})."
    )
