import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { apiErrorMessages, apiErrorStatus } from '../../core/api/api-error';
import { claimReference, formatMoney } from '../../core/claims/claim-format';
import {
  ClaimantClaim,
  ClaimantHistoryAction,
  ClaimantHistoryEntry,
  InformationRequestStatus,
} from '../../core/claims/claim.models';
import { ClaimantClaimsService } from '../../core/claims/claimant-claims.service';
import { marketLabel } from '../../core/claims/markets';
import { StatusBadge } from '../../shared/status-badge';

const ACTIONS: Record<ClaimantHistoryAction, string> = {
  Submitted: 'Claim submitted',
  PickedUp: 'Review started',
  InformationRequested: 'Information requested',
  InformationProvided: 'Information provided',
  Settled: 'Claim settled',
  Rejected: 'Claim rejected',
};

const REQUEST_STATUSES: Record<InformationRequestStatus, string> = {
  Open: 'Waiting for your answer',
  Answered: 'Answered',
  Closed: 'Closed without an answer',
};

interface ClaimView {
  claim: ClaimantClaim;
  history: ClaimantHistoryEntry[];
}

@Component({
  selector: 'app-claim-details',
  imports: [DatePipe, RouterLink, StatusBadge],
  templateUrl: './claim-details.html',
  styleUrl: './claim-details.css',
})
export class ClaimDetails implements OnInit {
  private readonly claimsApi = inject(ClaimantClaimsService);

  // From the route.
  readonly id = input.required<string>();

  // Null while loading.
  protected readonly view = signal<ClaimView | null>(null);
  protected readonly notFound = signal(false);
  protected readonly errors = signal<string[]>([]);

  // Set only when arriving from a successful submission.
  protected readonly submittedReference = inject(Router).currentNavigation()?.extras.state?.[
    'submittedReference'
  ] as string | undefined;

  protected readonly reference = claimReference;
  protected readonly market = marketLabel;
  protected readonly requestStatuses = REQUEST_STATUSES;

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.view.set(null);
    this.notFound.set(false);
    this.errors.set([]);

    if (!/^\d{1,9}$/.test(this.id())) {
      this.notFound.set(true);
      return;
    }

    const id = Number(this.id());
    forkJoin({ claim: this.claimsApi.get(id), history: this.claimsApi.history(id) }).subscribe({
      // The API returns history oldest first. Only events meant for claimants are shown.
      next: ({ claim, history }) =>
        this.view.set({ claim, history: history.filter((entry) => Object.hasOwn(ACTIONS, entry.action)) }),
      error: (error: unknown) => {
        // The API also answers 404 for another claimant's claim.
        if (apiErrorStatus(error) === 404) {
          this.notFound.set(true);
        } else {
          this.errors.set(apiErrorMessages(error));
        }
      },
    });
  }

  protected actionLabel(action: ClaimantHistoryAction): string {
    return ACTIONS[action];
  }

  protected money(claim: ClaimantClaim, amountMinor: number): string {
    return formatMoney(amountMinor, claim.currencyCode, claim.currencyDecimalPlaces);
  }
}
