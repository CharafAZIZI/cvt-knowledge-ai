import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AskRequest, AskResponse } from '../models/ai.model';

@Injectable({ providedIn: 'root' })
export class AiService {
  // Route Gateway définie dans ocelot.json : POST /api/ai/ask -> ai-service:8000/ask
  private readonly apiUrl = `${environment.apiUrl}/ai/ask`;

  constructor(private readonly http: HttpClient) {}

  ask(question: string): Observable<AskResponse> {
    const request: AskRequest = { question };
    return this.http.post<AskResponse>(this.apiUrl, request);
  }
}
