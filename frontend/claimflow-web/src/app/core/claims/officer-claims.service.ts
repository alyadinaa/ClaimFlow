import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API } from '../api/api.config';
import {
  CurrencyExposure,
  Dashboard,
  OfficerAction,
  OfficerClaim,
  OfficerHistoryEntry,
} from './officer.models';

@Injectable({ providedIn: 'root' })
export class OfficerClaimsService {
  private readonly http = inject(HttpClient);

  dashboard(): Observable<Dashboard> {
    return this.http.get<Dashboard>(API.dashboard);
  }

  exposure(): Observable<CurrencyExposure[]> {
    return this.http.get<CurrencyExposure[]>(`${API.dashboard}/exposure`);
  }

  list(): Observable<OfficerClaim[]> {
    return this.http.get<OfficerClaim[]>(API.officerClaims);
  }

  get(id: number): Observable<OfficerClaim> {
    return this.http.get<OfficerClaim>(`${API.officerClaims}/${id}`);
  }

  history(id: number): Observable<OfficerHistoryEntry[]> {
    return this.http.get<OfficerHistoryEntry[]>(`${API.officerClaims}/${id}/history`);
  }

  // Every action sends the version the officer was looking at.
  act(id: number, expectedVersion: number, action: OfficerAction): Observable<OfficerClaim> {
    const { path, ...details } = action;
    return this.http.post<OfficerClaim>(`${API.officerClaims}/${id}/${path}`, { expectedVersion, ...details });
  }
}
