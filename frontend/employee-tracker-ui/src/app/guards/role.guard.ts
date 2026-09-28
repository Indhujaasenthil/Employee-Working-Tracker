import { inject } from '@angular/core';
import {
  CanActivateFn,
  Router
} from '@angular/router';

import {
  AuthService
} from '../services/auth.service';

export const roleGuard: CanActivateFn = (
  route
) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return router.createUrlTree(['/login']);
  }

  const allowedRoles =
    route.data?.['roles'] as string[] | undefined;

  if (
    !allowedRoles ||
    allowedRoles.length === 0
  ) {
    return true;
  }

  const currentRole =
    authService.getRole()?.toLowerCase();

  const hasPermission =
    allowedRoles.some(
      role =>
        role.toLowerCase() === currentRole
    );

  if (hasPermission) {
    return true;
  }

  return router.createUrlTree(
    ['/dashboard'],
    {
      queryParams: {
        accessDenied: 'true'
      }
    }
  );
};