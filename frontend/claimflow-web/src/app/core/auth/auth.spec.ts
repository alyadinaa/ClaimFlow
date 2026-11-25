import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { NoticeService } from '../notice/notice.service';
import { guestGuard, roleGuard } from './auth.guards';
import { authInterceptor } from './auth.interceptor';
import { UserRole } from './auth.models';
import { AuthService } from './auth.service';

@Component({ template: '' })
class EmptyPage {}

function setUp(stored?: string) {
  sessionStorage.clear();
  if (stored !== undefined) {
    sessionStorage.setItem('claimflow.session', stored);
  }
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors([authInterceptor])),
      provideHttpClientTesting(),
      provideRouter([
        { path: 'login', component: EmptyPage, canActivate: [guestGuard] },
        { path: 'claimant', component: EmptyPage, canActivate: [roleGuard('Claimant')] },
        { path: 'officer', component: EmptyPage, canActivate: [roleGuard('ClaimsOfficer')] },
      ]),
    ],
  });
  return {
    auth: TestBed.inject(AuthService),
    http: TestBed.inject(HttpClient),
    api: TestBed.inject(HttpTestingController),
    router: TestBed.inject(Router),
    notice: TestBed.inject(NoticeService),
  };
}

function session(role: UserRole, minutes = 60) {
  return JSON.stringify({
    accessToken: 'test-token',
    expiresAtUtc: new Date(Date.now() + minutes * 60_000).toISOString(),
    displayName: 'Test User',
    role,
  });
}

describe('AuthService', () => {
  it('logs in, stores the session and knows the home page', () => {
    const { auth, api } = setUp();

    auth.login({ email: 'a@demo.example', password: 'x' }).subscribe();
    const request = api.expectOne('/api/auth/login');
    expect(request.request.body).toEqual({ email: 'a@demo.example', password: 'x' });
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush(JSON.parse(session('ClaimsOfficer')));

    expect(auth.role()).toBe('ClaimsOfficer');
    expect(auth.homeUrl()).toBe('/officer');
    expect(sessionStorage.getItem('claimflow.session')).not.toBeNull();
    expect(sessionStorage.getItem('claimflow.session')).not.toContain('"password"');
  });

  it('restores a stored session and clears it on logout', () => {
    const { auth } = setUp(session('Claimant'));
    expect(auth.homeUrl()).toBe('/claimant');

    auth.logout();

    expect(auth.role()).toBeNull();
    expect(sessionStorage.getItem('claimflow.session')).toBeNull();
  });

  it('ignores expired, broken and unknown-role sessions', () => {
    for (const stored of [session('Claimant', -1), 'not json', session('Manager' as UserRole)]) {
      TestBed.resetTestingModule();
      expect(setUp(stored).auth.role()).toBeNull();
    }
  });

  it('stops using a token once it expires', () => {
    const { auth } = setUp(session('Claimant', 1));
    expect(auth.token()).toBe('test-token');

    vi.spyOn(Date, 'now').mockReturnValue(Date.now() + 2 * 60_000);
    expect(auth.token()).toBeNull();
    vi.restoreAllMocks();
  });
});

describe('guards', () => {
  it.each([
    [undefined, '/claimant', '/login'],
    [undefined, '/officer', '/login'],
    [session('Claimant'), '/claimant', '/claimant'],
    [session('Claimant'), '/officer', '/claimant'],
    [session('ClaimsOfficer'), '/officer', '/officer'],
    [session('ClaimsOfficer'), '/claimant', '/officer'],
    [session('ClaimsOfficer'), '/login', '/officer'],
  ])('session %#: opening %s leads to %s', async (stored, target, expected) => {
    const { router } = setUp(stored);
    await router.navigateByUrl(target);
    expect(router.url).toBe(expected);
  });
});

describe('interceptor', () => {
  it('sends the token to the API but not to other sites', () => {
    const { http, api } = setUp(session('Claimant'));

    http.get('/api/claims').subscribe();
    expect(api.expectOne('/api/claims').request.headers.get('Authorization')).toBe('Bearer test-token');

    http.get('https://other.example/api/claims').subscribe();
    expect(api.expectOne('https://other.example/api/claims').request.headers.has('Authorization')).toBe(false);
  });

  it('ends the session on 401', async () => {
    const { auth, http, api, router } = setUp(session('Claimant'));
    await router.navigateByUrl('/claimant');

    http.get('/api/claims').subscribe({ error: () => undefined });
    api.expectOne('/api/claims').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(auth.role()).toBeNull();
    expect(auth.sessionExpired()).toBe(true);
    await vi.waitFor(() => expect(router.url).toBe('/login'));
  });

  it('keeps the session on 403 and shows the reason', () => {
    const { auth, http, api, notice } = setUp(session('ClaimsOfficer'));

    http.get('/api/claims').subscribe({ error: () => undefined });
    api
      .expectOne('/api/claims')
      .flush({ detail: 'The claim is assigned to another officer.' }, { status: 403, statusText: 'Forbidden' });

    expect(auth.role()).toBe('ClaimsOfficer');
    expect(notice.message()).toBe('The claim is assigned to another officer.');
  });

  it('treats a failed login as a wrong password, not an ended session', () => {
    const { auth, api } = setUp();
    let status = 0;

    auth.login({ email: 'a@demo.example', password: 'x' }).subscribe({ error: (e) => (status = e.status) });
    api.expectOne('/api/auth/login').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(status).toBe(401);
    expect(auth.sessionExpired()).toBe(false);
  });
});
