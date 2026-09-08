from io import BytesIO
from pypdf import PdfReader


def extract_pages(file_bytes: bytes) -> list[str]:
    """
    Extrait le texte du PDF, page par page.
    Retourne une liste où l'index i correspond au texte de la page (i + 1).
    On garde le découpage par page pour pouvoir citer la source précise
    ("Guide.pdf, page 4") dans les réponses du RAG.
    """
    reader = PdfReader(BytesIO(file_bytes))

    pages_text = []
    for page in reader.pages:
        text = page.extract_text() or ""
        pages_text.append(text.strip())

    return pages_text
