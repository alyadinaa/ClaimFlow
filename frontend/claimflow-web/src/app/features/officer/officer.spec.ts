import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../../app.routes';
import { authInterceptor } from '../../core/auth/auth.interceptor';
import { OfficerClaim, OfficerHistoryEntry } from '../../core/claims/officer.models';
import { formatDuration } from './officer-dashboard';

// Uses the real routes, guards, interceptor and pages. Only the API is replaced.
const ME = 2;
let api: HttpTestingController;
let harness: RouterTestingHarness;

function claim(changes: Partial<OfficerClaim> = {}): OfficerClaim {
  return {
    id: 7,
    claimantId: 1,
    claimantName: 'Demo Claimant',
    assignedOfficerId: ME,
    assignedOfficerName: 'Demo Officer',
    type: 'Motor',
    market: 'Singapore',
    currencyCode: 'SGD',
    currencyDecimalPlaces: 2,
    status: 'InReview',
    incidentDate: '2026-01-15',
    description: 'Rear bumper damage',
    vehicleRegistration: 'SGX1234A',
    propertyAddress: null,
    claimedAmountMinor: 125050,
    assessedAmountMinor: null,
    settlementAmountMinor: null,
    rejectionReason: null,
    submittedAtUtc: '2026-01-16T02:00:00Z',
    decidedAtUtc: null,
    version: 2,
    informationRequests: [],
    ...changes,
  };
}

const submitted = (changes: Partial<OfficerClaim> = {}) =>
  claim({ status: 'Submitted', assignedOfficerId: null, assignedOfficerName: null, version: 1, ...changes });

function event(id: number, action: OfficerHistoryEntry['action'], changes: Partial<OfficerHistoryEntry> = {}): OfficerHistoryEntry {
  return {
    id,
    action,
    fromStatus: null,
    toStatus: 'InReview',
    actorId: ME,
    actorName: 'Demo Officer',
    actorRole: 'ClaimsOfficer',
    occurredAtUtc: '2026-01-16T02:00:00Z',
    note: null,
    amountMinor: null,
    currencyCode: null,
    informationRequestId: null,
    ...changes,
  };
}

async function open(url: string, role: string | null = 'ClaimsOfficer') {
  sessionStorage.clear();
  if (role) {
    sessionStorage.setItem(
      'claimflow.session',
      JSON.stringify({
        accessToken: `x.${btoa(JSON.stringify({ sub: String(ME) }))}.x`,
        expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString(),
        displayName: 'Demo Officer',
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
const buttons = () => [...page().querySelectorAll<HTMLButtonElement>('main button')].map((b) => b.textContent!.trim());

async function click(label: string) {
  [...page().querySelectorAll<HTMLButtonElement>('button')].find((b) => b.textContent!.trim() === label)!.click();
  await settle();
}

async function type(value: string) {
  const input = page().querySelector<HTMLInputElement>('#action-input')!;
  input.value = value;
  input.dispatchEvent(new Event('input'));
  await settle();
}

async function submit() {
  page().querySelector('form')!.dispatchEvent(new Event('submit'));
  await settle();
}

async function flush(request: TestRequest, body: object | null, status = 200) {
  request.flush(body, { status, statusText: 'Status' });
  await settle();
}

async function loadDetails(current = claim(), history = [event(1, 'Submitted'), event(2, 'PickedUp')]) {
  api.expectOne('/api/officer/claims/7').flush(current);
  api.expectOne('/api/officer/claims/7/history').flush(history);
  await settle();
}

async function failDetails() {
  api.expectOne('/api/officer/claims/7').flush(null, { status: 502, statusText: 'Error' });
  api.match('/api/officer/claims/7/history');
  await settle();
}

async function openDetails(current = claim()) {
  await open('/officer/claims/7');
  await loadDetails(current);
}

const action = (path: string) => api.expectOne(`/api/officer/claims/7/${path}`);

afterEach(() => api.verify());

describe('officer route protection', () => {
  it.each(['/officer/dashboard', '/officer/incoming', '/officer/assigned', '/officer/claims/7'])('%s', async (target) => {
    await open(target, null);
    expect(url()).toBe('/login');

    TestBed.resetTestingModule();
    await open(target, 'Claimant');
    expect(url()).toBe('/claimant/claims');
    api.expectOne('/api/claims').flush([]);
  });
});

describe('Dashboard', () => {
  const dashboard = {
    totalClaims: 21,
    unassignedClaims: 3,
    openClaims: 9,
    decidedClaims: 12,
    claimsByStatus: [{ status: 'AwaitingInformation', count: 4 }],
    officers: [
      { officerId: ME, officerName: 'Demo Officer', openAssigned: 5, awaitingInformation: 1, decided: 7, averageSecondsToDecision: 93_600 },
      { officerId: 9, officerName: 'Other Officer', openAssigned: 0, awaitingInformation: 0, decided: 0, averageSecondsToDecision: null },
    ],
  };

  it('shows metrics, workload and exposure per currency without a total', async () => {
    await open('/officer/dashboard');
    expect(text()).toContain('Loading the dashboard');
    api.expectOne('/api/officer/dashboard').flush(dashboard);
    api.expectOne('/api/officer/dashboard/exposure').flush([
      { currencyCode: 'JPY', currencyDecimalPlaces: 0, openClaims: 1, exposureMinor: 10000 },
      { currencyCode: 'SGD', currencyDecimalPlaces: 2, openClaims: 2, exposureMinor: 250050 },
    ]);
    await settle();

    const cards = [...page().querySelectorAll('.cards .card')].map((c) => c.textContent!.trim());
    expect(cards).toEqual(['Total claims21', 'Unassigned3', 'Open9', 'Awaiting information4', 'Decided12', 'My open claims5']);
    const rows = [...page().querySelectorAll('tbody tr')].map((r) => [...r.children].map((c) => c.textContent!.trim()));
    expect(rows).toEqual([
      ['Demo Officer (you)', '5', '1', '7', '1 d 2 h'],
      ['Other Officer', '0', '0', '0', '–'],
      ['JPY', '1', 'JPY 10,000'],
      ['SGD', '2', 'SGD 2,500.50'],
    ]);
  });

  it('shows empty and error states', async () => {
    await open('/officer/dashboard');
    api.expectOne('/api/officer/dashboard').flush({ ...dashboard, officers: [] });
    api.expectOne('/api/officer/dashboard/exposure').flush([]);
    await settle();
    expect(text()).toContain('There are no open claims.');

    await click('Refresh');
    api.expectOne('/api/officer/dashboard').flush(null, { status: 500, statusText: 'Error' });
    api.match('/api/officer/dashboard/exposure');
    await settle();
    expect(text()).toContain('cannot be reached');
  });

  it.each([
    [0.3, 'Less than 1 min'],
    [300, '5 min'],
    [7800, '2 h 10 min'],
    [93_600, '1 d 2 h'],
  ])('formats %d seconds as %s', (seconds, expected) => expect(formatDuration(seconds)).toBe(expected));
});

describe('claim lists', () => {
  const all = [submitted({ id: 3 }), claim({ id: 4 }), claim({ id: 5, status: 'Settled' })];
  const links = () => [...page().querySelectorAll('tbody a')].map((a) => a.getAttribute('href'));

  it('separates incoming claims from my claims', async () => {
    await open('/officer/incoming');
    api.expectOne('/api/officer/claims').flush(all);
    await settle();
    expect(links()).toEqual(['/officer/claims/3']);
    expect(text()).toContain('SGD 1,250.50');

    TestBed.resetTestingModule();
    await open('/officer/assigned');
    api.expectOne('/api/officer/claims').flush(all);
    await settle();
    expect(links()).toEqual(['/officer/claims/4', '/officer/claims/5']);
    expect(buttons()).not.toContain('Pick up');
  });

  it('picks up a claim once and refreshes the list', async () => {
    await open('/officer/incoming');
    api.expectOne('/api/officer/claims').flush(all);
    await settle();
    await click('Pick up');
    await click('Pick up');

    const request = api.expectOne('/api/officer/claims/3/pickup');
    expect(request.request.body).toEqual({ expectedVersion: 1 });
    await flush(request, claim({ id: 3 }));
    api.expectOne('/api/officer/claims').flush(all.slice(1));
    await settle();
    expect(text()).toContain('CLM-000003 is now assigned to you.');
    expect(text()).toContain('There are no claims waiting');
  });

  it('explains a failed pickup and refreshes', async () => {
    await open('/officer/incoming');
    api.expectOne('/api/officer/claims').flush(all);
    await settle();
    await click('Pick up');
    await flush(api.expectOne('/api/officer/claims/3/pickup'), { detail: 'The claim has changed.' }, 409);
    api.expectOne('/api/officer/claims').flush([]);
    await settle();
    expect(text()).toContain('The claim has changed.');
    expect(text()).toContain('The list has been refreshed.');
  });

  it('shows an error', async () => {
    await open('/officer/assigned');
    api.expectOne('/api/officer/claims').flush(null, { status: 500, statusText: 'Error' });
    await settle();
    expect(text()).toContain('cannot be reached');
  });
});

describe('claim details', () => {
  it('shows the claim, requests and full history', async () => {
    await open('/officer/claims/7');
    await loadDetails(
      claim({
        assessedAmountMinor: 80000,
        informationRequests: [
          { id: 1, question: 'Any photos?', status: 'Answered', requestedAtUtc: '2026-01-17T02:00:00Z', response: 'None.', respondedAtUtc: '2026-01-18T02:00:00Z' },
        ],
      }),
      [event(1, 'Submitted', { actorName: 'Demo Claimant' }), event(2, 'AssessmentRecorded', { amountMinor: 80000 })],
    );
    for (const expected of ['CLM-000007', 'Demo Claimant', 'Demo Officer (you)', 'SGD 1,250.50', 'SGD 800.00', 'Any photos?', 'Answer: None.']) {
      expect(text()).toContain(expected);
    }
    expect([...page().querySelectorAll('li strong')].map((s) => s.textContent)).toEqual(['Claim submitted', 'Assessment recorded']);
  });

  it.each([
    ['unassessed', claim(), ['Record assessment', 'Request information', 'Reject']],
    ['assessed', claim({ assessedAmountMinor: 1 }), ['Record assessment', 'Request information', 'Settle', 'Reject']],
    ['awaiting', claim({ status: 'AwaitingInformation' }), ['Reject']],
    ['submitted', submitted(), ['Pick up claim']],
    ['settled', claim({ status: 'Settled' }), []],
    ['another officer', claim({ assignedOfficerId: 9, assignedOfficerName: 'Other Officer' }), []],
  ])('offers only allowed actions: %s', async (_, current, expected) => {
    await openDetails(current);
    expect(buttons()).toEqual(expected);
  });

  it('handles a missing claim, an invalid id and a server error', async () => {
    await open('/officer/claims/7');
    api.expectOne('/api/officer/claims/7').flush(null, { status: 404, statusText: 'Not Found' });
    api.match('/api/officer/claims/7/history');
    await settle();
    expect(text()).toContain('Claim not found');

    TestBed.resetTestingModule();
    await open('/officer/claims/abc');
    expect(text()).toContain('Claim not found');

    TestBed.resetTestingModule();
    await open('/officer/claims/7');
    await failDetails();
    expect(text()).toContain('cannot be reached');
  });
});

describe('actions', () => {
  it('picks up from the details page', async () => {
    await openDetails(submitted());
    await click('Pick up claim');
    await flush(action('pickup'), claim());
    await loadDetails();
    expect(text()).toContain('The claim is now assigned to you.');
    expect(buttons()).toContain('Record assessment');
  });

  it('records an assessment once, then uses the new version', async () => {
    await openDetails();
    await click('Record assessment');
    await type('800.5');
    await submit();
    await submit();
    const request = action('assessment');
    expect(request.request.body).toEqual({ expectedVersion: 2, amountMinor: 80050 });
    await flush(request, claim({ assessedAmountMinor: 80050, version: 3 }));
    await loadDetails(claim({ assessedAmountMinor: 80050, version: 3 }));
    expect(text()).toContain('The assessment has been saved.');
    expect(page().querySelector('form')).toBeNull();

    await click('Record assessment');
    await type('10');
    await submit();
    const second = action('assessment');
    expect(second.request.body).toEqual({ expectedVersion: 3, amountMinor: 1000 });
    await flush(second, claim({ version: 4 }));
    await loadDetails(claim({ version: 4 }));
  });

  it.each([
    ['', 2, 'up to 2 decimal places'],
    ['1.234', 2, 'up to 2 decimal places'],
    ['0', 2, 'Enter an amount from SGD 0.01'],
    ['10.5', 0, 'has no decimal places'],
  ])('refuses the amount "%s" with %i decimal places', async (value, places, message) => {
    await openDetails(claim({ currencyDecimalPlaces: places }));
    await click('Record assessment');
    await type(value);
    await submit();
    expect(text()).toContain(message);
  });

  it('asks the claimant a question', async () => {
    await openDetails();
    await click('Request information');
    await type('  ');
    await submit();
    expect(text()).toContain('Enter a question.');
    await type('x'.repeat(2001));
    await submit();
    expect(text()).toContain('no more than 2000 characters');

    await type(' Any photos? ');
    await submit();
    const request = action('information-requests');
    expect(request.request.body).toEqual({ expectedVersion: 2, question: 'Any photos?' });
    const asked = claim({
      status: 'AwaitingInformation',
      version: 3,
      informationRequests: [{ id: 1, question: 'Any photos?', status: 'Open', requestedAtUtc: '2026-01-17T02:00:00Z', response: null, respondedAtUtc: null }],
    });
    await flush(request, asked);
    await loadDetails(asked);
    expect(text()).toContain("Waiting for the claimant's answer");
    expect(buttons()).toEqual(['Reject']);
  });

  it('settles only after confirmation', async () => {
    const assessed = claim({ assessedAmountMinor: 80000, version: 3 });
    await openDetails(assessed);
    await click('Settle');
    await submit();
    expect(text()).toContain('Settle CLM-000007 for SGD 800.00? This cannot be undone.');
    await click('Back');
    expect(text()).not.toContain('cannot be undone');

    await type('Approved.');
    await submit();
    await click('Yes, settle');
    const request = action('settle');
    expect(request.request.body).toEqual({ expectedVersion: 3, note: 'Approved.' });
    const settled = claim({ status: 'Settled', assessedAmountMinor: 80000, settlementAmountMinor: 80000, version: 4 });
    await flush(request, settled);
    await loadDetails(settled);
    expect(text()).toContain('The claim has been settled.');
    expect(text()).toContain('Settlement amount');
    expect(buttons()).toEqual([]);
  });

  it.each(['InReview', 'AwaitingInformation'] as const)('rejects from %s with a reason and confirmation', async (status) => {
    await openDetails(claim({ status }));
    await click('Reject');
    await submit();
    expect(text()).toContain('Enter a rejection reason.');
    await type('Not covered.');
    await submit();
    expect(text()).toContain('Reject CLM-000007?');
    await click('Yes, reject');
    const request = action('reject');
    expect(request.request.body).toEqual({ expectedVersion: 2, reason: 'Not covered.' });
    const rejected = claim({ status: 'Rejected', rejectionReason: 'Not covered.', version: 3 });
    await flush(request, rejected);
    await loadDetails(rejected);
    expect(text()).toContain('The claim has been rejected.');
  });

  it.each([
    [400, 'Question is required.'],
    [403, 'The claim is assigned to another officer.'],
  ])('shows %i and keeps the text', async (status, detail) => {
    await openDetails();
    await click('Request information');
    await type('Any photos?');
    await submit();
    await flush(action('information-requests'), { detail }, status);
    expect(text()).toContain(detail);
    expect(page().querySelector<HTMLTextAreaElement>('#action-input')!.value).toBe('Any photos?');
  });

  it('after 409 shows the new amount and needs a new confirmation', async () => {
    await openDetails(claim({ assessedAmountMinor: 50000, version: 3 }));
    await click('Settle');
    await submit();
    await click('Yes, settle');
    await flush(action('settle'), { detail: 'The claim has changed.' }, 409);
    await loadDetails(claim({ assessedAmountMinor: 80000, version: 4 }));
    expect(text()).toContain('The claim has changed.');
    expect(text()).not.toContain('cannot be undone');

    await submit();
    expect(text()).toContain('Settle CLM-000007 for SGD 800.00?');
    await click('Yes, settle');
    const second = action('settle');
    expect(second.request.body.expectedVersion).toBe(4);
    await flush(second, claim({ status: 'Settled', version: 5 }));
    await loadDetails(claim({ status: 'Settled', version: 5 }));
  });

  it('never repeats an action whose result is unknown, and locks if it cannot reload', async () => {
    await openDetails();
    await click('Record assessment');
    await type('100');
    await submit();
    action('assessment').error(new ProgressEvent('error'));
    await settle();
    await failDetails();
    expect(text()).toContain('The latest claim could not be loaded.');
    expect(buttons()).toEqual(['Reload claim']);

    await click('Reload claim');
    await loadDetails(claim({ assessedAmountMinor: 10000, version: 3 }));
    expect(text()).toContain('SGD 100.00');
  });

  it('handles 404 and 401', async () => {
    await openDetails(submitted());
    await click('Pick up claim');
    await flush(action('pickup'), null, 404);
    expect(text()).toContain('Claim not found');

    TestBed.resetTestingModule();
    await openDetails(submitted());
    await click('Pick up claim');
    await flush(action('pickup'), null, 401);
    await vi.waitFor(() => expect(url()).toBe('/login'));
  });
});
