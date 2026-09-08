FROM python:3.11-slim

WORKDIR /app

# Dépendances système minimales requises par pypdf / sentence-transformers
RUN apt-get update && apt-get install -y --no-install-recommends \
    build-essential \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt .

# IMPORTANT : sentence-transformers installe torch avec le stack CUDA complet
# par défaut (torch + cudnn + cublas + nccl... ≈ 2 Go de paquets GPU), ce qui
# n'a aucun intérêt ici (ce service tourne en CPU-only) et fait souvent
# timeout pip sur une connexion normale. On installe d'abord la variante
# CPU-only de torch (≈ 200 Mo) depuis l'index officiel PyTorch, puis le reste
# des dépendances réutilise ce torch déjà installé sans le retélécharger.
RUN pip install --no-cache-dir --timeout 120 --retries 5 \
    torch==2.4.1 --index-url https://download.pytorch.org/whl/cpu
RUN pip install --no-cache-dir --timeout 120 --retries 5 -r requirements.txt

COPY app ./app

# Le modèle d'embeddings sera téléchargé au premier démarrage et mis en
# cache dans /root/.cache — le volume Docker le persiste entre redémarrages.

EXPOSE 8000

CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
