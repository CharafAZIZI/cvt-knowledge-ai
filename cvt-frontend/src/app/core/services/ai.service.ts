import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AskStreamEvent, ConversationDetail, ConversationSummary } from '../models/ai.model';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class AiService {
  // Route Gateway définie dans ocelot.json : /api/ai/* -> ai-service:8000/*
  private readonly baseUrl = `${environment.apiUrl}/ai`;

  private readonly authService = inject(AuthService);

  constructor(private readonly http: HttpClient) {}

  listConversations(): Observable<ConversationSummary[]> {
    return this.http.get<ConversationSummary[]>(`${this.baseUrl}/conversations`);
  }

  getConversation(id: number): Observable<ConversationDetail> {
    return this.http.get<ConversationDetail>(`${this.baseUrl}/conversations/${id}`);
  }

  deleteConversation(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/conversations/${id}`);
  }

  /**
   * Envoie une question et reçoit la réponse en streaming (SSE), token par
   * token. On utilise `fetch` plutôt que `HttpClient` : HttpClient n'expose
   * pas de façon simple un flux de réponse progressif compatible avec du
   * texte "text/event-stream", alors que `fetch` + `ReadableStream` le fait
   * nativement.
   *
   * `onEvent` est appelé pour chaque évènement reçu (start / token / done /
   * error). La promesse se résout une fois le flux terminé.
   */
  async streamAsk(
    question: string,
    conversationId: number | null,
    onEvent: (event: AskStreamEvent) => void,
  ): Promise<void> {
    const token = this.authService.getToken();

    let response: Response;
    try {
      response = await fetch(`${this.baseUrl}/ask`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: JSON.stringify({ question, conversation_id: conversationId }),
      });
    } catch {
      onEvent({
        type: 'error',
        detail: "Impossible de contacter l'assistant. Vérifiez votre connexion.",
      });
      return;
    }

    if (!response.ok || !response.body) {
      let detail = "L'assistant est momentanément indisponible. Réessayez dans un instant.";
      try {
        const body = await response.json();
        detail = body?.detail ?? detail;
      } catch {
        // corps non-JSON (ou vide) : on garde le message par défaut
      }
      onEvent({ type: 'error', detail });
      return;
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';

    while (true) {
      const { done, value } = await reader.read();
      if (done) {
        break;
      }

      buffer += decoder.decode(value, { stream: true });

      // Les évènements SSE sont séparés par une ligne vide.
      const rawEvents = buffer.split('\n\n');
      buffer = rawEvents.pop() ?? '';

      for (const rawEvent of rawEvents) {
        const dataLine = rawEvent.split('\n').find((line) => line.startsWith('data: '));
        if (!dataLine) {
          continue;
        }
        try {
          onEvent(JSON.parse(dataLine.slice('data: '.length)) as AskStreamEvent);
        } catch {
          // évènement malformé : on l'ignore plutôt que de casser tout le flux
        }
      }
    }
  }
}
