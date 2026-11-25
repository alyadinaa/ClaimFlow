import { Routes } from '@angular/router';
import { PlaceholderPage } from '../../shared/placeholder-page';
import { Account } from './account';
import { ClaimantLayout } from './claimant-layout';

export const claimantRoutes: Routes = [
  {
    path: '',
    component: ClaimantLayout,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'claims' },
      { path: 'claims', title: 'My Claims | ClaimFlow', component: PlaceholderPage, data: { heading: 'My Claims' } },
      { path: 'submit', title: 'Submit Claim | ClaimFlow', component: PlaceholderPage, data: { heading: 'Submit Claim' } },
      { path: 'account', title: 'Account | ClaimFlow', component: Account },
    ],
  },
];
