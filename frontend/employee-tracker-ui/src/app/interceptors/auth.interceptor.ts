import { inject } from '@angular/core';
import {
  HttpErrorResponse,
  HttpInterceptorFn
} from '@angular/common/http';
import {
  catchError,
  throwError
} from 'rxjs';

import {
  AuthService
} from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (
  request,
  next
) => {
  const authService = inject(AuthService);
  const token = authService.getToken();

  const authenticatedRequest = token
    ? request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      })
    : request;

  return next(authenticatedRequest).pipe(
    catchError((error: HttpErrorResponse) => {
      if (
        error.status === 401 &&
        !request.url.includes('/api/auth/login')
      ) {
        authService.logout();
      }

      return throwError(() => error);
    })
  );
};