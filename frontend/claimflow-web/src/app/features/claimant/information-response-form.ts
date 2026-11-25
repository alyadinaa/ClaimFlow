import { Component, ElementRef, inject, input, output, signal, viewChild } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';

// Matches the backend limit. The API checks it again.
const MAX_RESPONSE = 2000;

// Collects the answer. The page sends it, so the text stays here if sending fails.
@Component({
  selector: 'app-information-response-form',
  imports: [ReactiveFormsModule],
  templateUrl: './information-response-form.html',
  styleUrl: './information-response-form.css',
})
export class InformationResponseForm {
  readonly sending = input.required<boolean>();
  // True while it is unknown whether an earlier attempt was saved.
  readonly locked = input.required<boolean>();
  readonly errors = input.required<string[]>();

  readonly send = output<string>();
  readonly checkAgain = output<void>();

  protected readonly maxLength = MAX_RESPONSE;
  protected readonly answer = new FormControl('', {
    nonNullable: true,
    validators: [
      (control) => ((control.value as string).trim() ? null : { required: true }),
      Validators.maxLength(MAX_RESPONSE),
    ],
  });
  protected readonly value = toSignal(this.answer.valueChanges, { initialValue: '' });
  protected readonly submitted = signal(false);
  private readonly box = viewChild.required<ElementRef<HTMLTextAreaElement>>('box');

  protected submit(event: Event): void {
    event.preventDefault();
    if (this.sending() || this.locked()) {
      return;
    }

    this.submitted.set(true);
    if (this.answer.invalid) {
      this.box().nativeElement.focus();
      return;
    }
    this.send.emit(this.answer.value.trim());
  }
}
