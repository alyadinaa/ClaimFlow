import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API } from '../api/api.config';
import {
  ClaimantClaim,
  ClaimantHistoryEntry,
  ProvideInformationRequest,
  SubmitClaimRequest,
} from './claim.models';

@Injectable({ providedIn: 'root' })
export class ClaimantClaimsService {
  private readonly http = inject(HttpClient);

  list(): Observable<ClaimantClaim[]> {
    return this.http.get<ClaimantClaim[]>(API.claims);
  }

  get(id: number): Observable<ClaimantClaim> {
    return this.http.get<ClaimantClaim>(`${API.claims}/${id}`);
  }

  history(id: number): Observable<ClaimantHistoryEntry[]> {
    return this.http.get<ClaimantHistoryEntry[]>(`${API.claims}/${id}/history`);
  }

  submit(request: SubmitClaimRequest): Observable<ClaimantClaim> {
    return this.http.post<ClaimantClaim>(API.claims, request);
  }

  provideInformation(id: number, request: ProvideInformationRequest): Observable<ClaimantClaim> {
    return this.http.post<ClaimantClaim>(`${API.claims}/${id}/information`, request);
  }
}
