import { Component, ElementRef, computed, effect, inject, input, output, signal } from '@angular/core';
import { claimReference, formatMoney, toMinorUnits } from '../../core/claims/claim-format';
import { OfficerAction, OfficerClaim } from '../../core/claims/officer.models';

// These limits match the backend. The API checks every rule again.
const MAX_TEXT = 2000;
const MAX_AMOUNT_MINOR = 1_000_000_000_000;

type Form = 'assessment' | 'information-requests' | 'settle' | 'reject';

const LABELS: Record<Form, { open: string; field: string; submit: string }> = {
  assessment: { open: 'Record assessment', field: 'Assessed amount', submit: 'Save assessment' },
  'information-requests': { open: 'Request information', field: 'Question for the claimant', submit: 'Send question' },
  settle: { open: 'Settle', field: 'Note to the claimant (optional)', submit: 'Continue' },
  reject: { open: 'Reject', field: 'Reason, shown to the claimant', submit: 'Continue' },
};

// Offers the actions allowed for an officer's own claim, one form at a time.
// The page sends the action, so typed text stays here if sending fails.
@Component({
  selector: 'app-claim-actions',
  templateUrl: './claim-actions.html',
  styleUrl: './claim-actions.css',
})
export class ClaimActions {
  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly claim = input.required<OfficerClaim>();
  readonly busy = input.required<boolean>();
  // Counts successful actions; a change clears the form.
  readonly completed = input.required<number>();

  readonly act = output<OfficerAction>();

  protected readonly labels = LABELS;
  protected readonly open = signal<Form | null>(null);
  protected readonly text = signal('');
  protected readonly error = signal<string | null>(null);
  protected readonly confirming = signal(false);

  protected readonly forms = computed<Form[]>(() => {
    if (this.claim().status === 'AwaitingInformation') {
      return ['reject'];
    }
    return this.claim().assessedAmountMinor === null
      ? ['assessment', 'information-requests', 'reject']
      : ['assessment', 'information-requests', 'settle', 'reject'];
  });

  protected readonly confirmation = computed(() => {
    const claim = this.claim();
    return this.open() === 'settle'
      ? `Settle ${claimReference(claim.id)} for ${this.money(claim.assessedAmountMinor ?? 0)}?`
      : `Reject ${claimReference(claim.id)}?`;
  });

  constructor() {
    effect(() => {
      this.completed();
      this.show(null);
    });
    // A changed claim must be confirmed again, because the amount may differ.
    effect(() => {
      this.claim();
      this.confirming.set(false);
    });
  }

  protected show(form: Form | null): void {
    this.open.set(form);
    this.text.set('');
    this.error.set(null);
    this.confirming.set(false);
    if (form) {
      this.focus('#action-input');
    }
  }

  protected submit(event: Event): void {
    event.preventDefault();
    const form = this.open();
    if (!form || this.busy()) {
      return;
    }

    this.error.set(this.validate(form));
    if (this.error()) {
      this.focus('#action-input');
    } else if (form === 'settle' || form === 'reject') {
      this.confirming.set(true);
      this.focus('#confirm');
    } else {
      this.send(form);
    }
  }

  protected confirm(): void {
    const form = this.open();
    if (form && this.confirming() && !this.busy()) {
      this.send(form);
    }
  }

  private send(form: Form): void {
    const text = this.text().trim();
    switch (form) {
      case 'assessment':
        this.act.emit({ path: form, amountMinor: this.amount()! });
        break;
      case 'information-requests':
        this.act.emit({ path: form, question: text });
        break;
      case 'settle':
        this.act.emit({ path: form, note: text || null });
        break;
      case 'reject':
        this.act.emit({ path: form, reason: text });
        break;
    }
  }

  private validate(form: Form): string | null {
    if (form === 'assessment') {
      const amount = this.amount();
      if (amount === null) {
        const places = this.claim().currencyDecimalPlaces;
        return places === 0
          ? `Enter a whole number. ${this.claim().currencyCode} has no decimal places.`
          : `Enter a number with up to ${places} decimal places, without commas.`;
      }
      return amount < 1 || amount > MAX_AMOUNT_MINOR
        ? `Enter an amount from ${this.money(1)} to ${this.money(MAX_AMOUNT_MINOR)}.`
        : null;
    }
    if (form !== 'settle' && !this.text().trim()) {
      return form === 'reject' ? 'Enter a rejection reason.' : 'Enter a question.';
    }
    return this.text().length > MAX_TEXT ? `Use no more than ${MAX_TEXT} characters.` : null;
  }

  private amount(): number | null {
    return toMinorUnits(this.text(), this.claim().currencyDecimalPlaces);
  }

  private money(amountMinor: number): string {
    const { currencyCode, currencyDecimalPlaces } = this.claim();
    return formatMoney(amountMinor, currencyCode, currencyDecimalPlaces);
  }

  private focus(selector: string): void {
    // Runs after the form has been drawn.
    setTimeout(() => this.element.nativeElement.querySelector<HTMLElement>(selector)?.focus());
  }
}
