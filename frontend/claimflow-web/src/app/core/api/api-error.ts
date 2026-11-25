import { HttpErrorResponse } from '@angular/common/http';

// The API returns ProblemDetails: "detail" for a business rule, or "errors" for an unreadable request.
interface ProblemDetails {
  detail?: string;
  errors?: Record<string, string[]>;
}

export function apiErrorStatus(error: unknown): number {
  return error instanceof HttpErrorResponse ? error.status : 0;
}

export function apiErrorMessages(error: unknown): string[] {
  const status = apiErrorStatus(error);
  if (status === 401) {
    return ['Your session has ended. Please log in again.'];
  }

  // Only expected errors carry text written for users. Server errors do not.
  if (status >= 400 && status < 500) {
    const problem = (error as HttpErrorResponse).error as ProblemDetails | null;
    const fieldMessages = Object.values(problem?.errors ?? {}).flat();
    if (fieldMessages.length > 0) {
      return fieldMessages;
    }
    return [problem?.detail ?? 'The request could not be completed.'];
  }

  return ['ClaimFlow cannot be reached right now. Please try again shortly.'];
}
