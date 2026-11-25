import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-account',
  template: `
    <h1>Account</h1>
    <div class="card">
      <p>You are logged in as <strong>{{ auth.displayName() }}</strong>.</p>
      <p>Account type: Claimant</p>
    </div>
  `,
})
export class Account {
  protected readonly auth = inject(AuthService);
}
