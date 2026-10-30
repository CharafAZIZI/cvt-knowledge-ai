# AiService — CVT Knowledge AI

Service RAG simple : extraction PDF → chunking → embeddings locaux
(sentence-transformers) → ChromaDB (persisté sur disque) → réponse générée
par un LLM local via Ollama.

## Démarrage

```bash
docker compose up --build
```

**Étape manuelle obligatoire au premier lancement** : Ollama démarre sans
aucun modèle téléchargé. Une fois les conteneurs lancés, tire le modèle une
seule fois (il sera ensuite persisté dans le volume `ollama_data`) :

```bash
docker exec -it cvt-ollama ollama pull llama3.2:3b
```

Ce téléchargement prend quelques minutes selon la connexion (~2 Go). Les
démarrages suivants n'ont plus besoin de cette étape.

## Endpoints

- `GET /health` — vérification de disponibilité.
- `POST /ingest` (multipart) — `document_id`, `title`, `file_name`, `file`.
  Appelé automatiquement par DocumentService après chaque upload réussi.
  Peut aussi être appelé manuellement (Postman) pour ré-indexer un document
  déjà existant en base.
- `DELETE /ingest/{document_id}` — retire un document de l'index (appelé
  automatiquement lors d'une suppression côté DocumentService).
- `POST /ask` — `{ "question": "..." }` → `{ "answer": "...", "sources": [...] }`.
  Exposé via le Gateway sur `POST /api/ai/ask`.

## Notes de conception (prototype volontairement simple)

- Seuls les fichiers `.pdf` sont indexés — cohérent avec le cahier des
  charges (procédures, notes de service, etc. sont des PDF).
- Le chunking est un découpage par taille fixe en caractères avec léger
  chevauchement, page par page (pour permettre de citer "Fichier.pdf,
  page X" dans les sources).
- ChromaDB tourne en mode local persistant (fichier sur disque), pas de
  serveur vector DB séparé — inutile pour ce volume de documents.
- Le LLM tourne localement via Ollama (aucune clé API, aucun coût). Modèle
  par défaut : `llama3.2:3b`, changeable via la variable d'environnement
  `AI_OLLAMA_MODEL`.
