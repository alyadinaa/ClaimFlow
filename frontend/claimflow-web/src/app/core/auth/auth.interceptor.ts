import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { API, API_BASE } from '../api/api.config';
import { NoticeService } from '../notice/notice.service';
import { AuthService } from './auth.service';

// Sends the token to our own API only. Login needs no token, and its 401 means a wrong password.
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith(`${API_BASE}/`) || request.url === API.login) {
    return next(request);
  }

  const auth = inject(AuthService);
  const notice = inject(NoticeService);
  const token = auth.token();
  const authorised = token
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;

  return next(authorised).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        auth.expire();
      } else if (error instanceof HttpErrorResponse && error.status === 403) {
        // The user stays logged in: the problem is this action, not the session.
        notice.show(error.error?.detail ?? 'You do not have permission to do that.');
      }
      return throwError(() => error);
    }),
  );
};
