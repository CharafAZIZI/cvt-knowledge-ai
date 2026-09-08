from dataclasses import dataclass
from app.config import settings


@dataclass
class Chunk:
    text: str
    page_number: int


def chunk_pages(pages_text: list[str]) -> list[Chunk]:
    """
    Découpe simplement chaque page en morceaux de taille fixe (en caractères)
    avec un léger chevauchement, pour ne pas couper les phrases en plein
    milieu d'une idée. Volontairement basique : pas besoin d'un chunking
    "intelligent" (par phrase/paragraphe) pour un prototype.
    """
    chunks: list[Chunk] = []
    size = settings.chunk_size_chars
    overlap = settings.chunk_overlap_chars

    for page_index, page_text in enumerate(pages_text):
        page_number = page_index + 1

        if not page_text:
            continue

        start = 0
        while start < len(page_text):
            end = start + size
            piece = page_text[start:end].strip()

            if piece:
                chunks.append(Chunk(text=piece, page_number=page_number))

            if end >= len(page_text):
                break

            start = end - overlap

    return chunks
