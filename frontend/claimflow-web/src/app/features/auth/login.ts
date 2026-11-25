import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthService);

  // The length limits match the API.
  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    password: ['', [Validators.required, Validators.maxLength(200)]],
  });

  protected readonly showPassword = signal(false);
  protected readonly loading = signal(false);
  protected readonly submitted = signal(false);
  protected readonly error = signal<string | null>(null);

  protected submit(): void {
    this.submitted.set(true);
    this.error.set(null);
    if (this.form.invalid || this.loading()) {
      return;
    }

    this.loading.set(true);
    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => void this.router.navigateByUrl(this.auth.homeUrl()),
      error: (error: unknown) => {
        this.loading.set(false);
        this.form.controls.password.reset();
        this.submitted.set(false);
        this.error.set(messageFor(error));
      },
    });
  }
}

function messageFor(error: unknown): string {
  const status = error instanceof HttpErrorResponse ? error.status : 0;
  switch (status) {
    case 400:
      return 'Please check your email and password and try again.';
    case 401:
      return 'Email or password is incorrect.';
    default:
      return 'ClaimFlow cannot be reached right now. Please try again shortly.';
  }
}
