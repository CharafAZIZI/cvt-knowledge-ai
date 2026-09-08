// Reflète AiService.app.main.AskRequest
export interface AskRequest {
  question: string;
}

// Reflète AiService.app.main.Source
// IMPORTANT : FastAPI/Pydantic ne renomme pas les champs en camelCase par
// défaut. Les noms ci-dessous (snake_case) correspondent exactement au JSON
// réellement renvoyé par POST /ask (voir AiService/app/main.py, classe Source).
export interface AiSource {
  file_name: string;
  title: string;
  page_number: number;
}

// Reflète AiService.app.main.AskResponse
export interface AskResponse {
  answer: string;
  sources: AiSource[];
}

// Modèle interne pour l'affichage de la conversation (pas de contrepartie backend)
export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  sources?: AiSource[];
  isError?: boolean;
  createdAt: Date;
}
