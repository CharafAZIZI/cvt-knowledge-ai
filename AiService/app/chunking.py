from dataclasses import dataclass
from app.config import settings


@dataclass
class Chunk:
    text: str
    page_number: int


def chunk_pages(pages_text: list[str]) -> list[Chunk]:
    """
    Découpe chaque page en morceaux d'environ `chunk_size_chars` caractères,
    avec chevauchement, SANS couper un mot en deux.

    L'ancienne version tranchait au caractère près (`page_text[start:end]`),
    ce qui coupait fréquemment un mot en deux à la frontière d'un chunk
    (ex. "contrôle tech" / "nique du véhicule"). Une fois découpé en deux
    morceaux distincts et embeddés séparément, ce genre de mot tronqué perd
    une grande partie de son sens vectoriel, ce qui dégrade la pertinence
    des sources retrouvées par la recherche RAG — exactement le symptôme
    remonté ("sources pas exactes").

    On recule donc systématiquement la coupure jusqu'au dernier espace
    trouvé dans la fenêtre visée, pour ne jamais couper un mot.
    """
    chunks: list[Chunk] = []
    size = settings.chunk_size_chars
    overlap = settings.chunk_overlap_chars
    # Si un "mot" fait plus du quart de la taille du chunk (cas très rare :
    # texte sans espaces, tableau mal extrait...), on renonce à chercher un
    # espace et on coupe quand même, pour ne pas produire un chunk énorme.
    min_piece_len = max(1, size // 4)

    for page_index, page_text in enumerate(pages_text):
        page_number = page_index + 1

        # Normalise les espaces multiples/retours à la ligne issus de
        # l'extraction PDF, pour que la recherche du dernier espace soit
        # fiable et que le texte envoyé au LLM soit plus propre.
        text = " ".join(page_text.split())

        if not text:
            continue

        text_len = len(text)
        start = 0

        while start < text_len:
            end = min(start + size, text_len)

            if end < text_len:
                last_space = text.rfind(" ", start, end)
                if last_space > start + min_piece_len:
                    end = last_space

            piece = text[start:end].strip()

            if piece:
                chunks.append(Chunk(text=piece, page_number=page_number))

            if end >= text_len:
                break

            next_start = end - overlap
            if next_start <= start:
                next_start = end

            # Recule aussi le point de départ du chevauchement sur un
            # espace, pour ne pas redémarrer le chunk suivant en plein
            # milieu d'un mot.
            space_before = text.rfind(" ", 0, next_start)
            if space_before > start:
                next_start = space_before + 1

            start = next_start

    return chunks
