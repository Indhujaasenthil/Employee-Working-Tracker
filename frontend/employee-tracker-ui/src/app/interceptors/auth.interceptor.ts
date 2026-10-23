
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

  const isLoginRequest =
    request.url.includes('/api/auth/login');

  const token = isLoginRequest
    ? null
    : authService.getToken();

  // Attach JWT to authenticated API requests.
  const authenticatedRequest = token
    ? request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      })
    : request;

  return next(authenticatedRequest).pipe(
    catchError((error: HttpErrorResponse) => {

      // Logout only when a request sent with
      // our JWT is rejected as unauthorized.
      if (
        error.status === 401 &&
        token !== null &&
        !isLoginRequest
      ) {
        // Avoid clearing a newer session if
        // an older request fails after re-login.
        if (authService.getToken() === token) {
          authService.logout();
        }
      }

      // 403 means the user is authenticated
      // but does not have permission.
      // Do not logout for 403.

      return throwError(() => error);
    })
  );
};
