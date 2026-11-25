import { ClaimStatus, ClaimType, InformationRequest, Market } from './claim.models';

// These match the officer and dashboard DTOs in the backend.
export interface OfficerClaim {
  id: number;
  claimantId: number;
  claimantName: string;
  assignedOfficerId: number | null;
  assignedOfficerName: string | null;
  type: ClaimType;
  market: Market;
  currencyCode: string;
  currencyDecimalPlaces: number;
  status: ClaimStatus;
  incidentDate: string;
  description: string;
  vehicleRegistration: string | null;
  propertyAddress: string | null;
  claimedAmountMinor: number;
  assessedAmountMinor: number | null;
  settlementAmountMinor: number | null;
  rejectionReason: string | null;
  submittedAtUtc: string;
  decidedAtUtc: string | null;
  version: number;
  informationRequests: InformationRequest[];
}

export type OfficerHistoryAction =
  | 'Submitted'
  | 'PickedUp'
  | 'AssessmentRecorded'
  | 'InformationRequested'
  | 'InformationProvided'
  | 'Settled'
  | 'Rejected';

export interface OfficerHistoryEntry {
  id: number;
  action: OfficerHistoryAction;
  fromStatus: ClaimStatus | null;
  toStatus: ClaimStatus;
  actorId: number;
  actorName: string;
  actorRole: 'Claimant' | 'ClaimsOfficer';
  occurredAtUtc: string;
  note: string | null;
  amountMinor: number | null;
  currencyCode: string | null;
  informationRequestId: number | null;
}

export interface OfficerWorkload {
  officerId: number;
  officerName: string;
  openAssigned: number;
  awaitingInformation: number;
  decided: number;
  averageSecondsToDecision: number | null;
}

export interface Dashboard {
  totalClaims: number;
  unassignedClaims: number;
  openClaims: number;
  decidedClaims: number;
  claimsByStatus: { status: ClaimStatus; count: number }[];
  officers: OfficerWorkload[];
}

export interface CurrencyExposure {
  currencyCode: string;
  currencyDecimalPlaces: number;
  openClaims: number;
  exposureMinor: number;
}

// The actions an officer can take on a claim, with what the API needs besides expectedVersion.
export type OfficerAction =
  | { path: 'pickup' }
  | { path: 'assessment'; amountMinor: number }
  | { path: 'information-requests'; question: string }
  | { path: 'settle'; note: string | null }
  | { path: 'reject'; reason: string };
