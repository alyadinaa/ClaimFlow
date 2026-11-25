import { Routes } from '@angular/router';
import { OfficerClaimDetails } from './officer-claim-details';
import { OfficerClaims } from './officer-claims';
import { OfficerDashboard } from './officer-dashboard';
import { OfficerLayout } from './officer-layout';

export const officerRoutes: Routes = [
  {
    path: '',
    component: OfficerLayout,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', title: 'Dashboard | ClaimFlow', component: OfficerDashboard },
      { path: 'incoming', title: 'Incoming Claims | ClaimFlow', component: OfficerClaims, data: { list: 'incoming' } },
      { path: 'assigned', title: 'My Assigned Claims | ClaimFlow', component: OfficerClaims, data: { list: 'assigned' } },
      { path: 'claims/:id', title: 'Claim | ClaimFlow', component: OfficerClaimDetails },
    ],
  },
];
