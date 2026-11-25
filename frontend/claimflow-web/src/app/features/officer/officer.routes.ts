import { Routes } from '@angular/router';
import { PlaceholderPage } from '../../shared/placeholder-page';
import { OfficerLayout } from './officer-layout';

export const officerRoutes: Routes = [
  {
    path: '',
    component: OfficerLayout,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', title: 'Dashboard | ClaimFlow', component: PlaceholderPage, data: { heading: 'Dashboard' } },
      { path: 'incoming', title: 'Incoming Claims | ClaimFlow', component: PlaceholderPage, data: { heading: 'Incoming Claims' } },
      { path: 'assigned', title: 'My Assigned Claims | ClaimFlow', component: PlaceholderPage, data: { heading: 'My Assigned Claims' } },
    ],
  },
];
