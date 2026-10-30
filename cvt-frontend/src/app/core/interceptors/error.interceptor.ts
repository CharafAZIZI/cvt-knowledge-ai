import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../services/toast.service';
import { extractErrorMessage } from '../utils/http-error.util';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const toastService = inject(ToastService);

  const isLoginRequest = req.url.endsWith('/auth/login');

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        if (error.status === 401 && !isLoginRequest) {
          toastService.error('Votre session a expiré. Veuillez vous reconnecter.');
          authService.logout();
        } else if (error.status === 403) {
          toastService.error("Vous n'avez pas les droits nécessaires pour cette action.");
        } else if (error.status === 0) {
          toastService.error('Impossible de contacter le serveur. Vérifiez votre connexion.');
        } else if (!isLoginRequest && error.status >= 500) {
          toastService.error(extractErrorMessage(error, 'Une erreur serveur est survenue.'));
        }
      }
      return throwError(() => error);
    }),
  );
};
