import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { apiErrorMessages } from '../../core/api/api-error';
import { claimReference, formatMoney } from '../../core/claims/claim-format';
import { marketLabel } from '../../core/claims/markets';
import { OfficerClaimsService } from '../../core/claims/officer-claims.service';
import { OfficerClaim } from '../../core/claims/officer.models';
import { StatusBadge } from '../../shared/status-badge';

// One page for both lists; the route says which.
@Component({
  selector: 'app-officer-claims',
  imports: [DatePipe, RouterLink, StatusBadge],
  templateUrl: './officer-claims.html',
  styleUrl: '../../shared/claim-table.css',
})
export class OfficerClaims implements OnInit {
  private readonly claimsApi = inject(OfficerClaimsService);

  readonly list = input.required<'incoming' | 'assigned'>();

  // Null while loading.
  private readonly all = signal<OfficerClaim[] | null>(null);
  protected readonly errors = signal<string[]>([]);
  protected readonly actionErrors = signal<string[]>([]);
  protected readonly message = signal<{ id: number; text: string } | null>(null);
  protected readonly busy = signal(false);

  protected readonly incoming = computed(() => this.list() === 'incoming');
  // The API returns submitted claims and the officer's own claims.
  protected readonly claims = computed(
    () => this.all()?.filter((claim) => (claim.status === 'Submitted') === this.incoming()) ?? null,
  );

  protected readonly reference = claimReference;
  protected readonly market = marketLabel;

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.errors.set([]);
    this.claimsApi.list().subscribe({
      next: (claims) => this.all.set(claims),
      error: (error: unknown) => this.errors.set(apiErrorMessages(error)),
    });
  }

  protected pickUp(claim: OfficerClaim): void {
    if (this.busy()) {
      return;
    }
    this.busy.set(true);
    this.actionErrors.set([]);
    this.message.set(null);
    this.claimsApi.act(claim.id, claim.version, { path: 'pickup' }).subscribe({
      next: () => {
        this.busy.set(false);
        this.message.set({ id: claim.id, text: `${claimReference(claim.id)} is now assigned to you.` });
        this.load();
      },
      error: (error: unknown) => {
        this.busy.set(false);
        this.actionErrors.set([
          ...apiErrorMessages(error),
          'The list has been refreshed. Check My Assigned Claims before trying again.',
        ]);
        this.load();
      },
    });
  }

  protected money(claim: OfficerClaim): string {
    return formatMoney(claim.claimedAmountMinor, claim.currencyCode, claim.currencyDecimalPlaces);
  }
}
