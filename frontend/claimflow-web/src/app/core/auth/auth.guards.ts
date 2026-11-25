import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { UserRole } from './auth.models';
import { AuthService } from './auth.service';

// Guards keep users in the right area. They are not security: the API enforces access.
export function roleGuard(required: UserRole): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    // Without a session homeUrl is the login page; with another role it is that role's area.
    return auth.role() === required || inject(Router).parseUrl(auth.homeUrl());
  };
}

// Logged-in users do not need the login page.
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.role() === null || inject(Router).parseUrl(auth.homeUrl());
};
