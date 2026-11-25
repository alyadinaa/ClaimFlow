import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { Observable, forkJoin, map } from 'rxjs';
import { apiErrorMessages, apiErrorStatus } from '../../core/api/api-error';
import { claimReference, formatMoney } from '../../core/claims/claim-format';
import {
  ClaimantClaim,
  ClaimantHistoryAction,
  ClaimantHistoryEntry,
  InformationRequest,
  InformationRequestStatus,
} from '../../core/claims/claim.models';
import { ClaimantClaimsService } from '../../core/claims/claimant-claims.service';
import { marketLabel } from '../../core/claims/markets';
import { StatusBadge } from '../../shared/status-badge';
import { InformationResponseForm } from './information-response-form';

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

const SENT = 'Your answer has been sent. A claims officer will continue reviewing your claim.';

interface ClaimView {
  claim: ClaimantClaim;
  history: ClaimantHistoryEntry[];
}

@Component({
  selector: 'app-claim-details',
  imports: [DatePipe, RouterLink, StatusBadge, InformationResponseForm],
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

  // Answering an information request
  protected readonly sending = signal(false);
  protected readonly uncertain = signal(false);
  protected readonly responseErrors = signal<string[]>([]);
  protected readonly message = signal<string | null>(null);
  protected readonly warning = signal<string | null>(null);

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

    this.fetch().subscribe({
      next: (view) => this.view.set(view),
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

  protected sendAnswer(request: InformationRequest, response: string): void {
    const claim = this.view()?.claim;
    if (!claim || this.sending() || this.uncertain()) {
      return;
    }

    this.sending.set(true);
    this.clearMessages();
    this.claimsApi
      .provideInformation(claim.id, { expectedVersion: claim.version, response })
      .subscribe({
        next: (updated) => this.answerSaved(updated),
        error: (error: unknown) => this.answerFailed(error, request),
      });
  }

  // Finds out what happened to an answer, or what changed. It never sends the answer again.
  protected checkAnswer(request: InformationRequest): void {
    this.sending.set(true);
    this.fetch().subscribe({
      next: (view) => {
        this.sending.set(false);
        this.uncertain.set(false);
        this.responseErrors.set([]);
        this.view.set(view);

        const current = view.claim.informationRequests.find((r) => r.id === request.id);
        if (current?.status === 'Open') {
          this.responseErrors.set([
            'Your answer has not been saved. Please check the claim, then choose whether to send it again.',
          ]);
        } else if (current?.status === 'Answered') {
          this.message.set('This request has been answered. The saved answer is shown below.');
        } else {
          this.warning.set('This request was closed, so your answer was not saved.');
        }
      },
      error: (error: unknown) => {
        this.sending.set(false);
        if (apiErrorStatus(error) === 404) {
          this.notFound.set(true);
          return;
        }
        this.uncertain.set(true);
        this.responseErrors.set([
          'We could not confirm whether your answer was saved. Please check again before sending it.',
        ]);
      },
    });
  }

  protected actionLabel(action: ClaimantHistoryAction): string {
    return ACTIONS[action];
  }

  protected money(claim: ClaimantClaim, amountMinor: number): string {
    return formatMoney(amountMinor, claim.currencyCode, claim.currencyDecimalPlaces);
  }

  private answerSaved(updated: ClaimantClaim): void {
    this.sending.set(false);
    this.message.set(SENT);
    // Show the saved answer now, then load the history and any later change.
    this.view.update((view) => (view ? { ...view, claim: updated } : view));
    this.fetch().subscribe({
      next: (view) => this.view.set(view),
      error: () => this.warning.set('The latest history could not be loaded. Refresh the page to see it.'),
    });
  }

  private answerFailed(error: unknown, request: InformationRequest): void {
    const status = apiErrorStatus(error);
    if (status === 404) {
      this.sending.set(false);
      this.notFound.set(true);
    } else if (status === 409 || status === 0 || status >= 500) {
      // 409: the claim changed. No reply or a server error: the answer may have been saved.
      this.uncertain.set(true);
      this.checkAnswer(request);
    } else {
      this.sending.set(false);
      this.responseErrors.set(apiErrorMessages(error));
    }
  }

  private clearMessages(): void {
    this.responseErrors.set([]);
    this.message.set(null);
    this.warning.set(null);
  }

  // The API returns history oldest first. Only events meant for claimants are kept.
  private fetch(): Observable<ClaimView> {
    const id = Number(this.id());
    return forkJoin({ claim: this.claimsApi.get(id), history: this.claimsApi.history(id) }).pipe(
      map(({ claim, history }) => ({
        claim,
        history: history.filter((entry) => Object.hasOwn(ACTIONS, entry.action)),
      })),
    );
  }
}
