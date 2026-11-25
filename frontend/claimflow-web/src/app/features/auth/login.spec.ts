import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Login } from './login';

describe('Login', () => {
  let fixture: ComponentFixture<Login>;
  let page: HTMLElement;
  let api: HttpTestingController;

  beforeEach(async () => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      imports: [Login],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    fixture = TestBed.createComponent(Login);
    page = fixture.nativeElement;
    api = TestBed.inject(HttpTestingController);
    await fixture.whenStable();
  });

  const input = (id: string) => page.querySelector<HTMLInputElement>(`#${id}`)!;

  function type(id: string, value: string) {
    input(id).value = value;
    input(id).dispatchEvent(new Event('input'));
  }

  async function submit() {
    page.querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
  }

  async function submitValid() {
    type('email', 'claimant@demo.example');
    type('password', 'some-password');
    await submit();
  }

  it('shows validation messages without calling the API', async () => {
    await submit();
    expect(page.textContent).toContain('Enter your email.');
    expect(page.textContent).toContain('Enter your password.');

    type('email', 'not-an-email');
    await submit();
    expect(page.textContent).toContain('Enter a valid email address.');
    api.verify();
  });

  it('shows and hides the password', async () => {
    expect(input('password').type).toBe('password');
    page.querySelector<HTMLButtonElement>('.toggle')!.click();
    await fixture.whenStable();
    expect(input('password').type).toBe('text');
  });

  it('disables the button while loading, then opens the user area', async () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    await submitValid();
    const button = page.querySelector<HTMLButtonElement>('.submit')!;
    expect(button.disabled).toBe(true);
    expect(button.textContent).toContain('Logging in');

    api.expectOne('/api/auth/login').flush({
      accessToken: 'test-token',
      expiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
      displayName: 'Test User',
      role: 'Claimant',
    });
    expect(navigate).toHaveBeenCalledWith('/claimant');
  });

  it.each([
    [401, 'Email or password is incorrect.'],
    [500, 'ClaimFlow cannot be reached right now.'],
  ])('shows a friendly message for status %i and clears the password', async (status, message) => {
    await submitValid();
    api.expectOne('/api/auth/login').flush(null, { status, statusText: 'Error' });
    await fixture.whenStable();

    expect(page.textContent).toContain(message);
    expect(input('password').value).toBe('');
    expect(input('email').value).toBe('claimant@demo.example');
    expect(page.querySelector<HTMLButtonElement>('.submit')!.disabled).toBe(false);
  });
});
