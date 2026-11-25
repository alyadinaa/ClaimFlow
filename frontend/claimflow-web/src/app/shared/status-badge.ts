import { Component, computed, input } from '@angular/core';
import { ClaimStatus } from '../core/claims/claim.models';

const LABELS: Record<ClaimStatus, string> = {
  Submitted: 'Submitted',
  InReview: 'In review',
  AwaitingInformation: 'Information needed',
  Settled: 'Settled',
  Rejected: 'Rejected',
};

// The text carries the meaning; colour only supports it.
@Component({
  selector: 'app-status-badge',
  template: '<span [class]="status()">{{ label() }}</span>',
  styles: `
    span {
      display: inline-block;
      padding: 0.15rem 0.7rem;
      border-radius: 999px;
      background: #e8edf4;
      color: #26374f;
      font-size: 0.85rem;
      font-weight: 600;
      white-space: nowrap;
    }
    .InReview {
      background: #dcecfb;
      color: #124a80;
    }
    .AwaitingInformation {
      background: #fdeecb;
      color: #7a4a00;
    }
    .Settled {
      background: #d6f2ea;
      color: #0b5f50;
    }
    .Rejected {
      background: #fde3e1;
      color: #93190f;
    }
  `,
})
export class StatusBadge {
  readonly status = input.required<ClaimStatus>();
  protected readonly label = computed(() => LABELS[this.status()]);
}
