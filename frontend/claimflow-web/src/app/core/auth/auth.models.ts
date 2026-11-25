// These match the backend's UserRole, LoginRequest and LoginResponse.
export type UserRole = 'Claimant' | 'ClaimsOfficer';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAtUtc: string;
  displayName: string;
  role: UserRole;
}
