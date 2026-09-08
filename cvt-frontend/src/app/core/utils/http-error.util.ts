import { HttpErrorResponse } from '@angular/common/http';

/**
 * Extrait un message d'erreur lisible, quelle que soit la forme de la
 * réponse d'erreur renvoyée par les différents backends du projet :
 * - AuthService/DocumentService (ASP.NET) : { message: "..." } ou ValidationProblem { errors: {...} }
 * - AiService (FastAPI) : { detail: "..." }
 */
export function extractErrorMessage(error: HttpErrorResponse, fallback: string): string {
  const body = error.error;

  if (typeof body === 'string' && body.trim().length > 0) {
    return body;
  }

  if (body?.message) {
    return body.message;
  }

  if (body?.detail) {
    return typeof body.detail === 'string' ? body.detail : fallback;
  }

  if (body?.errors) {
    const firstField = Object.values(body.errors)[0];
    if (Array.isArray(firstField) && firstField.length > 0) {
      return firstField[0];
    }
  }

  return fallback;
}
