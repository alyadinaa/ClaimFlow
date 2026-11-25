import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../../app.routes';
import { authInterceptor } from '../../core/auth/auth.interceptor';
import { ClaimantClaim, ClaimantHistoryEntry, InformationRequest } from '../../core/claims/claim.models';

// Uses the real routes, guards, interceptor and page. Only the API is replaced.
let api: HttpTestingController;
let harness: RouterTestingHarness;

const ANSWERED: InformationRequest = {
  id: 1,
  question: 'Any photos?',
  status: 'Answered',
  requestedAtUtc: '2026-01-17T02:00:00Z',
  response: 'No photos.',
  respondedAtUtc: '2026-01-18T02:00:00Z',
};
const OPEN: InformationRequest = {
  id: 2,
  question: 'Any witnesses?',
  status: 'Open',
  requestedAtUtc: '2026-01-19T02:00:00Z',
  response: null,
  respondedAtUtc: null,
};

function claim(changes: Partial<ClaimantClaim> = {}): ClaimantClaim {
  return {
    id: 7,
    type: 'Motor',
    market: 'Singapore',
    currencyCode: 'SGD',
    currencyDecimalPlaces: 2,
    status: 'AwaitingInformation',
    incidentDate: '2026-01-15',
    description: 'Rear bumper damage',
    vehicleRegistration: 'SGX1234A',
    propertyAddress: null,
    claimedAmountMinor: 125050,
    settlementAmountMinor: null,
    rejectionReason: null,
    submittedAtUtc: '2026-01-16T02:00:00Z',
    decidedAtUtc: null,
    version: 5,
    informationRequests: [ANSWERED, OPEN],
    ...changes,
  };
}

const answeredClaim = (response = 'My neighbour saw it.') =>
  claim({
    status: 'InReview',
    version: 6,
    informationRequests: [ANSWERED, { ...OPEN, status: 'Answered', response, respondedAtUtc: '2026-01-20T02:00:00Z' }],
  });

const rejectedClaim = () =>
  claim({ status: 'Rejected', version: 6, rejectionReason: 'No reply.', informationRequests: [ANSWERED, { ...OPEN, status: 'Closed' }] });

function history(...actions: string[]): ClaimantHistoryEntry[] {
  return actions.map((action) => ({
    action: action as ClaimantHistoryEntry['action'],
    fromStatus: null,
    toStatus: 'InReview',
    actorRole: 'Claimant',
    occurredAtUtc: '2026-01-16T02:00:00Z',
    note: null,
    amountMinor: null,
    currencyCode: null,
  }));
}

async function open(role: string | null = 'Claimant') {
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
  harness = await RouterTestingHarness.create('/claimant/claims/7');
  await settle();
}

const settle = () => harness.fixture.whenStable();
const page = () => harness.fixture.nativeElement as HTMLElement;
const text = () => page().textContent!.replace(/\s+/g, ' ');
const box = () => page().querySelector<HTMLTextAreaElement>('#response');
const button = () => page().querySelector<HTMLButtonElement>('button[type="submit"]')!;

async function loadClaim(current = claim(), events = history('Submitted', 'PickedUp', 'InformationRequested')) {
  api.expectOne('/api/claims/7').flush(current);
  api.expectOne('/api/claims/7/history').flush(events);
  await settle();
}

async function failLoad() {
  api.expectOne('/api/claims/7').flush(null, { status: 502, statusText: 'Error' });
  api.match('/api/claims/7/history');
  await settle();
}

async function type(value: string) {
  box()!.value = value;
  box()!.dispatchEvent(new Event('input'));
  await settle();
}

async function submit() {
  page().querySelector('form')!.dispatchEvent(new Event('submit'));
  await settle();
}

async function send(value = 'My neighbour saw it.'): Promise<TestRequest> {
  await type(value);
  await submit();
  return api.expectOne('/api/claims/7/information');
}

async function fail(request: TestRequest, status: number, body: object | null = null) {
  request.flush(body, { status, statusText: 'Error' });
  await settle();
}

afterEach(() => api.verify());

describe('information request display', () => {
  it('shows requests in order, with one form for the open request', async () => {
    await open();
    await loadClaim();

    const articles = [...page().querySelectorAll('article')].map((a) => a.textContent!.replace(/\s+/g, ' '));
    expect(articles).toHaveLength(2);
    expect(articles[0]).toContain('Answered');
    expect(articles[0]).toContain('Any photos?');
    expect(articles[0]).toContain('Your answer: No photos.');
    expect(articles[1]).toContain('Waiting for your answer');
    expect(articles[1]).toContain('Any witnesses?');
    expect(page().querySelectorAll('textarea')).toHaveLength(1);
    expect(page().querySelector('label[for="response"]')!.textContent).toBe('Your answer');
  });

  it('shows no form for answered and closed requests', async () => {
    await open();
    await loadClaim(rejectedClaim());
    expect(text()).toContain('Closed without an answer');
    expect(box()).toBeNull();
  });

  it('never shows officer-only data', async () => {
    await open();
    const leaked = { ...OPEN, requestedById: 99, requestedByName: 'Secret Officer' };
    await loadClaim(claim({ informationRequests: [leaked] }), history('Submitted', 'AssessmentRecorded'));
    expect(text()).not.toContain('Secret Officer');
    expect(text()).not.toContain('99');
    expect(text().toLowerCase()).not.toContain('assess');
  });

  it.each([null, 'ClaimsOfficer'])('is only for claimants (role %s)', async (role) => {
    await open(role);
    expect(TestBed.inject(Router).url).toBe(role ? '/officer/dashboard' : '/login');
  });
});

describe('validation', () => {
  it.each(['', '   \n '])('refuses a blank answer', async (value) => {
    await open();
    await loadClaim();
    await type(value);
    await submit();
    expect(text()).toContain('Enter your answer.');
    expect(box()!.getAttribute('aria-invalid')).toBe('true');
  });

  it('counts characters and refuses more than 2,000', async () => {
    await open();
    await loadClaim();
    await type('x'.repeat(2001));
    expect(text()).toContain('2001 of 2000 characters');
    expect(text()).toContain('Use no more than 2000 characters.');
    await submit();

    await type('x'.repeat(2000));
    expect(text()).not.toContain('Use no more than');
  });
});

describe('sending an answer', () => {
  it('sends once with the version shown, then refreshes the claim and history', async () => {
    await open();
    await loadClaim();
    const request = await send('  My neighbour saw it. ');
    expect(request.request.body).toEqual({ expectedVersion: 5, response: 'My neighbour saw it.' });
    expect(button().disabled).toBe(true);
    expect(text()).toContain('Sending');
    await submit();
    await submit();

    request.flush(answeredClaim());
    await settle();
    expect(text()).toContain('Your answer has been sent.');
    expect(text()).toContain('Your answer: My neighbour saw it.');
    expect(box()).toBeNull();

    await loadClaim(answeredClaim(), history('Submitted', 'PickedUp', 'InformationRequested', 'InformationProvided'));
    expect(text()).toContain('In review');
    expect(text()).toContain('Information provided');
    expect(text()).not.toMatch(/settled|approved|rejected/i);
  });

  it('keeps the saved answer on screen if the refresh fails', async () => {
    await open();
    await loadClaim();
    (await send()).flush(answeredClaim());
    await settle();
    await failLoad();
    expect(text()).toContain('Your answer has been sent.');
    expect(text()).toContain('Your answer: My neighbour saw it.');
    expect(text()).toContain('The latest history could not be loaded.');
  });
});

describe('errors', () => {
  it.each([
    [400, { detail: 'Response is required.' }, 'Response is required.'],
    [400, { errors: { ExpectedVersion: ['The ExpectedVersion field is required.'] } }, 'The ExpectedVersion field is required.'],
    [403, { detail: 'Your role cannot perform this action.' }, 'Your role cannot perform this action.'],
  ])('shows %i and keeps the answer', async (status, body, message) => {
    await open();
    await loadClaim();
    await fail(await send(), status, body);
    expect(text()).toContain(message);
    expect(box()!.value).toBe('My neighbour saw it.');
    expect(button().disabled).toBe(false);
  });

  it('shows not found for 404', async () => {
    await open();
    await loadClaim();
    await fail(await send(), 404);
    expect(text()).toContain('Claim not found');
  });

  it('returns to login for 401', async () => {
    await open();
    await loadClaim();
    await fail(await send(), 401);
    await vi.waitFor(() => expect(TestBed.inject(Router).url).toBe('/login'));
    await settle();
    expect(text()).toContain('Your session has ended');
  });

  it('refreshes after 409 and lets the claimant decide, using the new version', async () => {
    await open();
    await loadClaim();
    await fail(await send(), 409, { detail: 'The claim has changed.' });
    await loadClaim(claim({ version: 9 }));
    expect(text()).toContain('Your answer has not been saved.');
    expect(box()!.value).toBe('My neighbour saw it.');

    await submit();
    const second = api.expectOne('/api/claims/7/information');
    expect(second.request.body.expectedVersion).toBe(9);
    second.flush(answeredClaim());
    await loadClaim(answeredClaim());
  });

  it('explains a 409 when the request was closed', async () => {
    await open();
    await loadClaim();
    await fail(await send(), 409);
    await loadClaim(rejectedClaim());
    expect(text()).toContain('This request was closed, so your answer was not saved.');
    expect(text()).toContain('Reason: No reply.');
    expect(box()).toBeNull();
  });

  it('does not carry an answer over to a different question', async () => {
    await open();
    await loadClaim();
    await fail(await send(), 409);
    await loadClaim(
      claim({
        version: 8,
        informationRequests: [
          { ...OPEN, status: 'Answered', response: 'Sent from another tab' },
          { ...OPEN, id: 3, question: 'Which road?' },
        ],
      }),
    );
    expect(text()).toContain('This request has been answered.');
    expect(text()).toContain('Which road?');
    expect(box()!.value).toBe('');
  });
});

describe('uncertain result', () => {
  it.each([0, 500, 502])('after status %i it checks the claim and never resends by itself', async (status) => {
    await open();
    await loadClaim();
    const request = await send();
    if (status === 0) {
      request.error(new ProgressEvent('error'));
      await settle();
    } else {
      await fail(request, status, { detail: 'internal text' });
    }
    expect(button().disabled).toBe(true);

    await loadClaim();
    expect(text()).toContain('Your answer has not been saved.');
    expect(text()).not.toContain('internal text');
    expect(box()!.value).toBe('My neighbour saw it.');
    expect(button().disabled).toBe(false);
  });

  it('shows the saved answer if it had been saved', async () => {
    await open();
    await loadClaim();
    (await send()).error(new ProgressEvent('error'));
    await settle();
    await loadClaim(answeredClaim());
    expect(text()).toContain('This request has been answered.');
    expect(text()).toContain('Your answer: My neighbour saw it.');
    expect(box()).toBeNull();
  });

  it('stays locked until a check succeeds', async () => {
    await open();
    await loadClaim();
    (await send()).error(new ProgressEvent('error'));
    await settle();
    await failLoad();
    expect(text()).toContain('We could not confirm whether your answer was saved.');
    expect(box()!.value).toBe('My neighbour saw it.');
    expect(button().disabled).toBe(true);
    await submit();

    page().querySelector<HTMLButtonElement>('.alert button')!.click();
    await loadClaim();
    expect(button().disabled).toBe(false);
  });
});
