import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateDocumentPayload,
  CvtDocument,
  UpdateDocumentPayload,
} from '../models/document.model';

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private readonly apiUrl = `${environment.apiUrl}/documents`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<CvtDocument[]> {
    return this.http.get<CvtDocument[]>(this.apiUrl);
  }

  getById(id: number): Observable<CvtDocument> {
    return this.http.get<CvtDocument>(`${this.apiUrl}/${id}`);
  }

  search(searchTerm: string): Observable<CvtDocument[]> {
    return this.http.get<CvtDocument[]>(`${this.apiUrl}/search`, {
      params: { searchTerm },
    });
  }

  /**
   * Le backend attend explicitement du multipart/form-data avec les champs
   * "title", "description" et "file" (voir DocumentsController.Create).
   */
  create(payload: CreateDocumentPayload): Observable<CvtDocument> {
    const formData = new FormData();
    formData.append('title', payload.title);
    formData.append('description', payload.description ?? '');
    formData.append('file', payload.file);

    return this.http.post<CvtDocument>(this.apiUrl, formData);
  }

  /**
   * Même logique que create() : multipart obligatoire. Le champ "file" n'est
   * envoyé que si l'utilisateur remplace réellement le fichier existant
   * (voir DocumentsController.Update : file est optionnel côté backend).
   */
  update(id: number, payload: UpdateDocumentPayload): Observable<CvtDocument> {
    const formData = new FormData();
    formData.append('title', payload.title);
    formData.append('description', payload.description ?? '');
    if (payload.file) {
      formData.append('file', payload.file);
    }

    return this.http.put<CvtDocument>(`${this.apiUrl}/${id}`, formData);
  }

  delete(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${id}`);
  }

  /** Télécharge le fichier binaire du document (GET /api/documents/{id}/download). */
  download(id: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${id}/download`, {
      responseType: 'blob',
    });
  }
}
