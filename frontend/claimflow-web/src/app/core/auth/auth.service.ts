import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { API } from '../api/api.config';
import { LoginRequest, LoginResponse, UserRole } from './auth.models';

const STORAGE_KEY = 'claimflow.session';

const HOME: Record<UserRole, string> = {
  Claimant: '/claimant',
  ClaimsOfficer: '/officer',
};

// The session is kept in sessionStorage, so it survives a refresh and ends when the tab closes.
// This only controls what the screens show. The API checks every request itself.
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly session = signal<LoginResponse | null>(readSession());

  readonly displayName = computed(() => this.session()?.displayName ?? '');
  readonly sessionExpired = signal(false);

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(API.login, request).pipe(
      tap((response) => {
        sessionStorage.setItem(STORAGE_KEY, JSON.stringify(response));
        this.session.set(response);
        this.sessionExpired.set(false);
      }),
    );
  }

  logout(): void {
    this.clear();
    this.sessionExpired.set(false);
    void this.router.navigateByUrl('/login');
  }

  // Called when the API no longer accepts the token.
  expire(): void {
    this.clear();
    this.sessionExpired.set(true);
    void this.router.navigateByUrl('/login');
  }

  // These are methods, not signals, because the answer also depends on the time.
  token(): string | null {
    return this.current()?.accessToken ?? null;
  }

  role(): UserRole | null {
    return this.current()?.role ?? null;
  }

  homeUrl(): string {
    const role = this.role();
    return role ? HOME[role] : '/login';
  }

  private current(): LoginResponse | null {
    const session = this.session();
    return session && isCurrent(session) ? session : null;
  }

  private clear(): void {
    sessionStorage.removeItem(STORAGE_KEY);
    this.session.set(null);
  }
}

function isCurrent(session: LoginResponse): boolean {
  return Date.parse(session.expiresAtUtc) > Date.now();
}

function readSession(): LoginResponse | null {
  try {
    const value = JSON.parse(sessionStorage.getItem(STORAGE_KEY) ?? 'null') as LoginResponse | null;
    return value?.accessToken && value.role in HOME && isCurrent(value) ? value : null;
  } catch {
    return null;
  }
}
