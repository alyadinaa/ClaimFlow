// Relative, so requests go to the site's own origin. In development, proxy.conf.json forwards them.
export const API_BASE = '/api';

export const API = {
  login: `${API_BASE}/auth/login`,
  officerClaims: `${API_BASE}/officer/claims`,
  dashboard: `${API_BASE}/officer/dashboard`,
  claims: `${API_BASE}/claims`,
} as const;
