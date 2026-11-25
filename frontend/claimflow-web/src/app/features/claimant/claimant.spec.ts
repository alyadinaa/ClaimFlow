import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../../app.routes';
import { authInterceptor } from '../../core/auth/auth.interceptor';
import { ClaimantClaim, ClaimantHistoryEntry } from '../../core/claims/claim.models';

// These tests use the real routes, guards, interceptor and pages. Only the API is replaced.
let api: HttpTestingController;
let harness: RouterTestingHarness;

function claim(changes: Partial<ClaimantClaim> = {}): ClaimantClaim {
  return {
    id: 7,
    type: 'Motor',
    market: 'HongKong',
    currencyCode: 'HKD',
    currencyDecimalPlaces: 2,
    status: 'Submitted',
    incidentDate: '2026-01-15',
    description: 'Rear bumper damage',
    vehicleRegistration: 'HK1234',
    propertyAddress: null,
    claimedAmountMinor: 125050,
    settlementAmountMinor: null,
    rejectionReason: null,
    submittedAtUtc: '2026-01-16T02:00:00Z',
    decidedAtUtc: null,
    version: 1,
    informationRequests: [],
    ...changes,
  };
}

function event(changes: Partial<ClaimantHistoryEntry> = {}): ClaimantHistoryEntry {
  return {
    action: 'Submitted',
    fromStatus: null,
    toStatus: 'Submitted',
    actorRole: 'Claimant',
    occurredAtUtc: '2026-01-16T02:00:00Z',
    note: null,
    amountMinor: null,
    currencyCode: null,
    ...changes,
  };
}

async function open(url: string, role: string | null = 'Claimant') {
  sessionStorage.clear();
  if (role) {
    sessionStorage.setItem(
      'claimflow.session',
      JSON.stringify({
        accessToken: 'test-token',
        expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString(),
        displayName: 'Test Claimant',
        role,
      }),
    );
  }
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors([authInterceptor])),
      provideHttpClientTesting(),
      provideRouter(routes, withComponentInputBinding()),
    ],
  });
  api = TestBed.inject(HttpTestingController);
  harness = await RouterTestingHarness.create(url);
  await settle();
}

const settle = () => harness.fixture.whenStable();
const page = () => harness.fixture.nativeElement as HTMLElement;
const text = () => page().textContent!.replace(/\s+/g, ' ');
const url = () => TestBed.inject(Router).url;
const field = (id: string) => page().querySelector<HTMLInputElement>(`#${id}`);

async function set(id: string, value: string) {
  const element = field(id)!;
  element.value = value;
  element.dispatchEvent(new Event(element.tagName === 'SELECT' ? 'change' : 'input'));
  await settle();
}

async function choose(type: string) {
  page().querySelector<HTMLInputElement>(`input[value="${type}"]`)!.click();
  await settle();
}

async function submit() {
  page().querySelector('form')!.dispatchEvent(new Event('submit'));
  await settle();
}

async function openDetails(theClaim: ClaimantClaim, history: ClaimantHistoryEntry[] = [event()]) {
  await open(`/claimant/claims/${theClaim.id}`);
  api.expectOne(`/api/claims/${theClaim.id}`).flush(theClaim);
  api.expectOne(`/api/claims/${theClaim.id}/history`).flush(history);
  await settle();
}

afterEach(() => api.verify());

describe('route protection', () => {
  it.each(['/claimant/claims', '/claimant/claims/7', '/claimant/submit'])('%s needs a claimant', async (target) => {
    await open(target, null);
    expect(url()).toBe('/login');

    TestBed.resetTestingModule();
    await open(target, 'ClaimsOfficer');
    expect(url()).toBe('/officer/dashboard');
    api.expectOne('/api/officer/dashboard');
    api.expectOne('/api/officer/dashboard/exposure');
  });
});

describe('My Claims', () => {
  it('shows loading, then the claims from the API', async () => {
    await open('/claimant/claims');
    expect(text()).toContain('Loading your claims');

    const request = api.expectOne('/api/claims');
    expect(request.request.headers.get('Authorization')).toBe('Bearer test-token');
    request.flush([claim(), claim({ id: 6, type: 'Property', status: 'AwaitingInformation' })]);
    await settle();

    const rows = [...page().querySelectorAll('tbody tr')].map((r) => r.textContent!.replace(/\s+/g, ' '));
    expect(rows).toHaveLength(2);
    for (const expected of ['CLM-000007', 'Motor', 'Hong Kong', '15 Jan 2026', '16 Jan 2026', 'Submitted']) {
      expect(rows[0]).toContain(expected);
    }
    expect(rows[1]).toContain('Information needed');
    expect(page().querySelector('tbody a')!.getAttribute('href')).toBe('/claimant/claims/7');
    expect(text()).toContain('Submit a claim');
  });

  it('shows an empty state', async () => {
    await open('/claimant/claims');
    api.expectOne('/api/claims').flush([]);
    await settle();
    expect(text()).toContain('No claims yet');
  });

  it('shows an error and can try again', async () => {
    await open('/claimant/claims');
    api.expectOne('/api/claims').flush(null, { status: 500, statusText: 'Error' });
    await settle();
    expect(text()).toContain('cannot be reached');

    page().querySelector<HTMLButtonElement>('.alert button')!.click();
    api.expectOne('/api/claims').flush([claim()]);
    await settle();
    expect(text()).toContain('CLM-000007');
  });
});

describe('Submit Claim', () => {
  async function fillMotor() {
    await set('market', 'Singapore');
    await set('incidentDate', '2026-01-15');
    await set('vehicleRegistration', ' SGX1234A ');
    await set('description', 'Rear bumper damage');
    await set('claimedAmount', '1250.50');
  }

  it('shows only the fields for the chosen type', async () => {
    await open('/claimant/submit');
    expect(field('vehicleRegistration')).not.toBeNull();
    expect(field('propertyAddress')).toBeNull();

    await choose('Property');
    expect(field('vehicleRegistration')).toBeNull();
    expect(field('propertyAddress')).not.toBeNull();
    expect(page().querySelector('input[type="file"]')).toBeNull();
  });

  it('explains missing fields without calling the API', async () => {
    await open('/claimant/submit');
    await submit();
    for (const message of [
      'Choose a market.',
      'Enter the incident date.',
      'Enter the vehicle registration.',
      'Describe what happened.',
      'Enter the amount you are claiming.',
    ]) {
      expect(text()).toContain(message);
    }

    await choose('Property');
    await submit();
    expect(text()).toContain('Enter the property address.');
    expect(text()).not.toContain('Enter the vehicle registration.');
  });

  it('checks date, length and amount', async () => {
    await open('/claimant/submit');
    await fillMotor();
    await set('incidentDate', '2999-01-01');
    await set('vehicleRegistration', 'x'.repeat(21));
    await set('description', '   ');
    await set('claimedAmount', '0');
    await submit();
    expect(text()).toContain('cannot be in the future');
    expect(text()).toContain('no more than 20 characters');
    expect(text()).toContain('Describe what happened.');
    expect(text()).toContain('Enter an amount from SGD 0.01 to SGD 10,000,000,000.00.');

    await set('claimedAmount', '1.234');
    expect(text()).toContain('up to 2 decimal places');

    // The same text is checked again when the market, and so the currency, changes.
    await set('claimedAmount', '100.50');
    expect(field('claimedAmount')!.getAttribute('aria-invalid')).toBe('false');
    await set('market', 'Japan');
    expect(text()).toContain('JPY has no decimal places');
  });

  it('submits a motor claim once and opens it', async () => {
    await open('/claimant/submit');
    await fillMotor();
    await submit();
    expect(page().querySelector<HTMLButtonElement>('button[type="submit"]')!.disabled).toBe(true);
    expect(text()).toContain('Submitting');
    await submit();

    const request = api.expectOne('/api/claims');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      type: 'Motor',
      market: 'Singapore',
      incidentDate: '2026-01-15',
      description: 'Rear bumper damage',
      vehicleRegistration: 'SGX1234A',
      propertyAddress: null,
      claimedAmountMinor: 125050,
    });
    request.flush(claim({ id: 8 }), { status: 201, statusText: 'Created' });
    await vi.waitFor(() => expect(url()).toBe('/claimant/claims/8'));

    api.expectOne('/api/claims/8').flush(claim({ id: 8 }));
    api.expectOne('/api/claims/8/history').flush([event()]);
    await settle();
    expect(text()).toContain('Your claim CLM-000008 has been submitted.');
  });

  it('submits a property claim in a currency without decimals', async () => {
    await open('/claimant/submit');
    await fillMotor();
    await choose('Property');
    await set('market', 'Japan');
    await set('propertyAddress', '1 Example Street');
    await set('claimedAmount', '10000');
    await submit();

    const request = api.expectOne('/api/claims');
    expect(request.request.body).toMatchObject({
      type: 'Property',
      market: 'Japan',
      vehicleRegistration: null,
      propertyAddress: '1 Example Street',
      claimedAmountMinor: 10000,
    });
    request.flush(null, { status: 500, statusText: 'Error' });
  });

  it.each([
    [400, { detail: 'Incident date cannot be in the future.' }, 'Incident date cannot be in the future.'],
    [400, { errors: { Market: ['The Market field is required.'] } }, 'The Market field is required.'],
    [500, { detail: 'internal text' }, 'ClaimFlow cannot be reached right now.'],
  ])('shows API error %i and keeps the form', async (status, body, message) => {
    await open('/claimant/submit');
    await fillMotor();
    await submit();
    api.expectOne('/api/claims').flush(body, { status, statusText: 'Error' });
    await settle();

    expect(text()).toContain('Your claim was not submitted.');
    expect(text()).toContain(message);
    expect(text()).not.toContain('internal text');
    expect(field('description')!.value).toBe('Rear bumper damage');
    expect(page().querySelector<HTMLButtonElement>('button[type="submit"]')!.disabled).toBe(false);
  });
});

describe('Claim details', () => {
  it('shows the claim and its history in order', async () => {
    await openDetails(claim({ status: 'InReview' }), [
      event(),
      event({ action: 'PickedUp', fromStatus: 'Submitted', toStatus: 'InReview', actorRole: 'ClaimsOfficer' }),
    ]);

    for (const expected of ['CLM-000007', 'In review', 'Hong Kong', 'HK1234', 'HKD 1,250.50', 'Rear bumper damage']) {
      expect(text()).toContain(expected);
    }
    const steps = [...page().querySelectorAll('li strong')].map((s) => s.textContent);
    expect(steps).toEqual(['Claim submitted', 'Review started']);
    expect(text()).not.toContain('has been submitted');
    expect(text()).not.toContain('Information requests');
  });

  it('shows information requests', async () => {
    await openDetails(
      claim({
        status: 'AwaitingInformation',
        informationRequests: [
          { id: 1, question: 'Any photos?', status: 'Answered', requestedAtUtc: '2026-01-17T02:00:00Z', response: 'None.', respondedAtUtc: '2026-01-18T02:00:00Z' },
          { id: 2, question: 'Any witnesses?', status: 'Open', requestedAtUtc: '2026-01-19T02:00:00Z', response: null, respondedAtUtc: null },
        ],
      }),
    );
    for (const expected of ['Any photos?', 'Your answer: None.', 'Any witnesses?', 'Waiting for your answer']) {
      expect(text()).toContain(expected);
    }
  });

  it('shows the settlement and the rejection reason', async () => {
    await openDetails(claim({ status: 'Settled', settlementAmountMinor: 80000 }), [
      event({ action: 'Settled', toStatus: 'Settled', actorRole: 'ClaimsOfficer', amountMinor: 80000, currencyCode: 'HKD', note: 'Approved.' }),
    ]);
    expect(text()).toContain('settled for HKD 800.00');
    expect(text()).toContain('Approved.');

    TestBed.resetTestingModule();
    await openDetails(claim({ status: 'Rejected', rejectionReason: 'Not covered.' }));
    expect(text()).toContain('Reason: Not covered.');
  });

  // The API never sends this data to claimants. The page must not show it even if it did.
  it('never shows officer-only data', async () => {
    const leaked = { ...claim({ status: 'InReview' }), assessedAmountMinor: 54321, assignedOfficerName: 'Secret Officer' };
    const assessment = { ...event(), action: 'AssessmentRecorded', amountMinor: 54321, actorName: 'Secret Officer' };
    await openDetails(leaked, [event(), assessment as unknown as ClaimantHistoryEntry]);

    expect(text()).not.toContain('543.21');
    expect(text()).not.toContain('Secret Officer');
    expect(text().toLowerCase()).not.toContain('assess');
    expect(page().querySelectorAll('li')).toHaveLength(1);
  });

  it('handles a missing claim, an invalid id and a server error', async () => {
    await open('/claimant/claims/99');
    api.expectOne('/api/claims/99').flush(null, { status: 404, statusText: 'Not Found' });
    api.match('/api/claims/99/history');
    await settle();
    expect(text()).toContain('Claim not found');

    TestBed.resetTestingModule();
    await open('/claimant/claims/abc');
    expect(text()).toContain('Claim not found');

    TestBed.resetTestingModule();
    await open('/claimant/claims/7');
    api.expectOne('/api/claims/7').flush(null, { status: 500, statusText: 'Error' });
    api.match('/api/claims/7/history');
    await settle();
    expect(text()).toContain('cannot be reached');
  });

  it('returns to login when the session is no longer accepted', async () => {
    await open('/claimant/claims/7');
    api.expectOne('/api/claims/7').flush(null, { status: 401, statusText: 'Unauthorized' });
    api.match('/api/claims/7/history');
    await vi.waitFor(() => expect(url()).toBe('/login'));
    await settle();
    expect(text()).toContain('Your session has ended');
  });
});
