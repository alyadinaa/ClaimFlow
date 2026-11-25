import { Component } from '@angular/core';
import { NavLink, Shell } from '../../shared/shell';

@Component({
  selector: 'app-officer-layout',
  imports: [Shell],
  template: '<app-shell area="Claims officer" [links]="links" />',
})
export class OfficerLayout {
  protected readonly links: NavLink[] = [
    { label: 'Dashboard', path: '/officer/dashboard' },
    { label: 'Incoming Claims', path: '/officer/incoming' },
    { label: 'My Assigned Claims', path: '/officer/assigned' },
  ];
}
