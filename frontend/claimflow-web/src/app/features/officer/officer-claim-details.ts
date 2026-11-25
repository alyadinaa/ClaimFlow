import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { Observable, forkJoin } from 'rxjs';
import { apiErrorMessages, apiErrorStatus } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { claimReference, formatMoney } from '../../core/claims/claim-format';
import { InformationRequestStatus } from '../../core/claims/claim.models';
import { marketLabel } from '../../core/claims/markets';
import { OfficerClaimsService } from '../../core/claims/officer-claims.service';
import {
  OfficerAction,
  OfficerClaim,
  OfficerHistoryAction,
  OfficerHistoryEntry,
} from '../../core/claims/officer.models';
import { StatusBadge } from '../../shared/status-badge';
import { ClaimActions } from './claim-actions';

const ACTIONS: Record<OfficerHistoryAction, string> = {
  Submitted: 'Claim submitted',
  PickedUp: 'Picked up',
  AssessmentRecorded: 'Assessment recorded',
  InformationRequested: 'Information requested',
  InformationProvided: 'Information provided',
  Settled: 'Settled',
  Rejected: 'Rejected',
};

const REQUEST_STATUSES: Record<InformationRequestStatus, string> = {
  Open: "Waiting for the claimant's answer",
  Answered: 'Answered',
  Closed: 'Closed without an answer',
};

const DONE: Record<OfficerAction['path'], string> = {
  pickup: 'The claim is now assigned to you.',
  assessment: 'The assessment has been saved.',
  'information-requests': 'The question has been sent to the claimant.',
  settle: 'The claim has been settled.',
  reject: 'The claim has been rejected.',
};

interface ClaimView {
  claim: OfficerClaim;
  history: OfficerHistoryEntry[];
}

@Component({
  selector: 'app-officer-claim-details',
  imports: [DatePipe, StatusBadge, ClaimActions],
  templateUrl: './officer-claim-details.html',
  styleUrl: '../../shared/claim-details.css',
})
export class OfficerClaimDetails implements OnInit {
  private readonly claimsApi = inject(OfficerClaimsService);
  private readonly userId = inject(AuthService).userId();

  // From the route.
  readonly id = input.required<string>();

  // Null while loading.
  protected readonly view = signal<ClaimView | null>(null);
  protected readonly notFound = signal(false);
  protected readonly errors = signal<string[]>([]);

  protected readonly busy = signal(false);
  // True when it is unknown what the claim looks like now, so no action is offered.
  protected readonly locked = signal(false);
  protected readonly completed = signal(0);
  protected readonly message = signal<string | null>(null);
  protected readonly actionErrors = signal<string[]>([]);

  protected readonly isMine = computed(() => {
    const claim = this.view()?.claim;
    return !!claim && this.userId !== null && claim.assignedOfficerId === this.userId;
  });
  protected readonly isOpen = computed(() => {
    const status = this.view()?.claim.status;
    return status === 'InReview' || status === 'AwaitingInformation';
  });

  protected readonly reference = claimReference;
  protected readonly market = marketLabel;
  protected readonly actions = ACTIONS;
  protected readonly requestStatuses = REQUEST_STATUSES;

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.view.set(null);
    this.notFound.set(false);
    this.errors.set([]);
    this.actionErrors.set([]);
    this.locked.set(false);

    if (!/^\d{1,9}$/.test(this.id())) {
      this.notFound.set(true);
      return;
    }

    this.fetch().subscribe({
      next: (view) => this.view.set(view),
      error: (error: unknown) => {
        if (apiErrorStatus(error) === 404) {
          this.notFound.set(true);
        } else {
          this.errors.set(apiErrorMessages(error));
        }
      },
    });
  }

  protected act(action: OfficerAction): void {
    const claim = this.view()?.claim;
    if (!claim || this.busy() || this.locked()) {
      return;
    }

    this.busy.set(true);
    this.message.set(null);
    this.actionErrors.set([]);
    this.claimsApi.act(claim.id, claim.version, action).subscribe({
      next: (updated) => {
        this.message.set(DONE[action.path]);
        this.completed.update((count) => count + 1);
        // Show the result now, then load the history and any later change.
        this.view.update((view) => (view ? { ...view, claim: updated } : view));
        this.refresh();
      },
      error: (error: unknown) => {
        const status = apiErrorStatus(error);
        if (status === 404) {
          this.busy.set(false);
          this.notFound.set(true);
        } else if (status >= 400 && status < 500 && status !== 409) {
          this.busy.set(false);
          this.actionErrors.set(apiErrorMessages(error));
        } else {
          // The claim changed, or the result is unknown: the action may have been saved.
          this.actionErrors.set(
            status === 409
              ? [...apiErrorMessages(error), 'The claim below is up to date. Check it before trying again.']
              : [
                  'We could not confirm whether the action was saved.',
                  'The claim below is up to date. Check it before trying again.',
                ],
          );
          this.refresh();
        }
      },
    });
  }

  protected money(claim: OfficerClaim, amountMinor: number): string {
    return formatMoney(amountMinor, claim.currencyCode, claim.currencyDecimalPlaces);
  }

  // Never repeats the action. Without fresh data, actions stay locked.
  private refresh(): void {
    this.fetch().subscribe({
      next: (view) => {
        this.view.set(view);
        this.busy.set(false);
      },
      error: () => {
        this.busy.set(false);
        this.locked.set(true);
        this.actionErrors.set(['The latest claim could not be loaded. Reload it before doing anything else.']);
      },
    });
  }

  // The API returns history oldest first.
  private fetch(): Observable<ClaimView> {
    const id = Number(this.id());
    return forkJoin({ claim: this.claimsApi.get(id), history: this.claimsApi.history(id) });
  }
}
