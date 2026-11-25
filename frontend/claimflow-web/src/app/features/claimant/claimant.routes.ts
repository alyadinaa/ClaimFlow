import { Routes } from '@angular/router';
import { Account } from './account';
import { ClaimDetails } from './claim-details';
import { ClaimantLayout } from './claimant-layout';
import { MyClaims } from './my-claims';
import { SubmitClaim } from './submit-claim';

export const claimantRoutes: Routes = [
  {
    path: '',
    component: ClaimantLayout,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'claims' },
      { path: 'claims', title: 'My Claims | ClaimFlow', component: MyClaims },
      { path: 'claims/:id', title: 'Claim | ClaimFlow', component: ClaimDetails },
      { path: 'submit', title: 'Submit Claim | ClaimFlow', component: SubmitClaim },
      { path: 'account', title: 'Account | ClaimFlow', component: Account },
    ],
  },
];
