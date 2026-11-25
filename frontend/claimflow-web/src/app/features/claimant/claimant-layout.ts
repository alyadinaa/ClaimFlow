import { Component } from '@angular/core';
import { NavLink, Shell } from '../../shared/shell';

@Component({
  selector: 'app-claimant-layout',
  imports: [Shell],
  template: '<app-shell area="Claimant" [links]="links" />',
})
export class ClaimantLayout {
  protected readonly links: NavLink[] = [
    { label: 'My Claims', path: '/claimant/claims' },
    { label: 'Submit Claim', path: '/claimant/submit' },
    { label: 'Account', path: '/claimant/account' },
  ];
}
