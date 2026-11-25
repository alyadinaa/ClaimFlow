import { Routes } from '@angular/router';
import { guestGuard, roleGuard } from './core/auth/auth.guards';

// Each area is loaded only when it is opened.
export const routes: Routes = [
  {
    path: 'login',
    title: 'Log in | ClaimFlow',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login').then((m) => m.Login),
  },
  {
    path: 'claimant',
    canActivate: [roleGuard('Claimant')],
    loadChildren: () => import('./features/claimant/claimant.routes').then((m) => m.claimantRoutes),
  },
  {
    path: 'officer',
    canActivate: [roleGuard('ClaimsOfficer')],
    loadChildren: () => import('./features/officer/officer.routes').then((m) => m.officerRoutes),
  },
  { path: '**', redirectTo: 'login' },
];
