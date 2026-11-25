import { Component, ElementRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { apiErrorMessages } from '../../core/api/api-error';
import { claimReference, formatMoney, toMinorUnits } from '../../core/claims/claim-format';
import { ClaimType, Market } from '../../core/claims/claim.models';
import { ClaimantClaimsService } from '../../core/claims/claimant-claims.service';
import { MARKETS, findMarket, marketToday } from '../../core/claims/markets';

// These limits match the backend. The API checks every rule again.
const MAX_DESCRIPTION = 2000;
const MAX_REGISTRATION = 20;
const MAX_ADDRESS = 300;
const MAX_AMOUNT_MINOR = 1_000_000_000_000;

const notBlank: ValidatorFn = (control) => ((control.value as string).trim() ? null : { required: true });

function text(maxLength: number): ValidatorFn[] {
  return [notBlank, Validators.maxLength(maxLength)];
}

// The rules for date and amount depend on the chosen market.
function selectedMarket(control: AbstractControl) {
  return findMarket((control.parent?.get('market')?.value as string | undefined) ?? '');
}

function notInFuture(control: AbstractControl): ValidationErrors | null {
  const market = selectedMarket(control);
  return market && control.value > marketToday(market) ? { future: true } : null;
}

function validAmount(control: AbstractControl): ValidationErrors | null {
  const market = selectedMarket(control);
  if (!market || !control.value) {
    return null;
  }
  const minor = toMinorUnits(control.value as string, market.decimalPlaces);
  if (minor === null) {
    return { format: true };
  }
  return minor < 1 || minor > MAX_AMOUNT_MINOR ? { range: true } : null;
}

@Component({
  selector: 'app-submit-claim',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './submit-claim.html',
  styleUrl: './submit-claim.css',
})
export class SubmitClaim {
  private readonly claimsApi = inject(ClaimantClaimsService);
  private readonly router = inject(Router);
  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);

  protected readonly markets = MARKETS;
  protected readonly maxDescription = MAX_DESCRIPTION;

  protected readonly form = inject(NonNullableFormBuilder).group({
    type: ['Motor' as ClaimType],
    market: ['' as Market | '', Validators.required],
    incidentDate: ['', [Validators.required, notInFuture]],
    description: ['', text(MAX_DESCRIPTION)],
    vehicleRegistration: ['', text(MAX_REGISTRATION)],
    propertyAddress: ['', text(MAX_ADDRESS)],
    claimedAmount: ['', [notBlank, validAmount]],
  });

  protected readonly type = toSignal(this.form.controls.type.valueChanges, { initialValue: 'Motor' });
  private readonly marketValue = toSignal(this.form.controls.market.valueChanges, { initialValue: '' });
  protected readonly market = computed(() => findMarket(this.marketValue()));
  protected readonly latestDate = computed(() => {
    const market = this.market();
    return market ? marketToday(market) : null;
  });

  protected readonly submitted = signal(false);
  protected readonly loading = signal(false);
  protected readonly errors = signal<string[]>([]);

  constructor() {
    const { type, market, incidentDate, claimedAmount } = this.form.controls;
    this.useFieldsFor('Motor');
    type.valueChanges.pipe(takeUntilDestroyed()).subscribe((value) => this.useFieldsFor(value));
    market.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      incidentDate.updateValueAndValidity();
      claimedAmount.updateValueAndValidity();
    });
  }

  protected submit(): void {
    if (this.loading()) {
      return;
    }

    this.submitted.set(true);
    this.errors.set([]);
    const market = this.market();
    const value = this.form.getRawValue();
    const amount = market ? toMinorUnits(value.claimedAmount, market.decimalPlaces) : null;
    if (this.form.invalid || !market || amount === null) {
      this.focusFirstError();
      return;
    }

    const isMotor = value.type === 'Motor';
    this.loading.set(true);
    this.claimsApi
      .submit({
        type: value.type,
        market: market.value,
        incidentDate: value.incidentDate,
        description: value.description.trim(),
        vehicleRegistration: isMotor ? value.vehicleRegistration.trim() : null,
        propertyAddress: isMotor ? null : value.propertyAddress.trim(),
        claimedAmountMinor: amount,
      })
      .subscribe({
        // The button stays disabled until the next page opens.
        next: (claim) =>
          void this.router.navigate(['/claimant/claims', claim.id], {
            state: { submittedReference: claimReference(claim.id) },
          }),
        error: (error: unknown) => {
          this.loading.set(false);
          this.errors.set(apiErrorMessages(error));
        },
      });
  }

  // Returns the message for a field once the user has tried to submit.
  protected error(name: keyof SubmitClaim['form']['controls']): string | null {
    const control = this.form.controls[name];
    if (!this.submitted() || !control.invalid) {
      return null;
    }
    if (control.hasError('required')) {
      return REQUIRED_MESSAGES[name];
    }
    if (control.hasError('maxlength')) {
      const { requiredLength } = control.getError('maxlength') as { requiredLength: number };
      return `Use no more than ${requiredLength} characters.`;
    }
    if (control.hasError('future')) {
      return 'The incident date cannot be in the future.';
    }
    return this.amountMessage(control.hasError('range'));
  }

  private amountMessage(outOfRange: boolean): string {
    const { currencyCode, decimalPlaces } = this.market()!;
    if (outOfRange) {
      const money = (minor: number) => formatMoney(minor, currencyCode, decimalPlaces);
      return `Enter an amount from ${money(1)} to ${money(MAX_AMOUNT_MINOR)}.`;
    }
    return decimalPlaces === 0
      ? `Enter a whole number. ${currencyCode} has no decimal places.`
      : `Enter a number with up to ${decimalPlaces} decimal places, without commas.`;
  }

  // A disabled control is not validated, so only the field for the chosen type counts.
  private useFieldsFor(type: ClaimType): void {
    const { vehicleRegistration, propertyAddress } = this.form.controls;
    const [used, unused] =
      type === 'Motor' ? [vehicleRegistration, propertyAddress] : [propertyAddress, vehicleRegistration];
    used.enable();
    unused.reset();
    unused.disable();
  }

  private focusFirstError(): void {
    // Runs after the error messages have been drawn.
    setTimeout(() => this.element.nativeElement.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus());
  }
}

const REQUIRED_MESSAGES: Record<keyof SubmitClaim['form']['controls'], string> = {
  type: 'Choose a claim type.',
  market: 'Choose a market.',
  incidentDate: 'Enter the incident date.',
  description: 'Describe what happened.',
  vehicleRegistration: 'Enter the vehicle registration.',
  propertyAddress: 'Enter the property address.',
  claimedAmount: 'Enter the amount you are claiming.',
};
