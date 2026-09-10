// Reflète AiService.app.main.AskRequest
export interface AskRequest {
  question: string;
  conversation_id?: number | null;
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

// Reflète AiService.app.history_store (liste : GET /ai/conversations)
export interface ConversationSummary {
  id: number;
  title: string;
  created_at: string;
  updated_at: string;
}

// Message tel que renvoyé par GET /ai/conversations/{id}
export interface StoredMessage {
  role: 'user' | 'assistant';
  content: string;
  sources: AiSource[];
  created_at: string;
}

// Reflète la réponse de GET /ai/conversations/{id}
export interface ConversationDetail {
  id: number;
  messages: StoredMessage[];
}

// Un évènement du flux SSE renvoyé par POST /ai/ask
export interface AskStreamEvent {
  type: 'start' | 'token' | 'done' | 'error';
  content?: string;
  conversation_id?: number;
  sources?: AiSource[];
  detail?: string;
}
