// These match the backend enums and the claimant DTOs in ClaimDtos.cs.
export type ClaimType = 'Motor' | 'Property';

export type ClaimStatus = 'Submitted' | 'InReview' | 'AwaitingInformation' | 'Settled' | 'Rejected';

export type InformationRequestStatus = 'Open' | 'Answered' | 'Closed';

export type Market =
  | 'Malaysia'
  | 'Singapore'
  | 'Australia'
  | 'HongKong'
  | 'Japan'
  | 'NewZealand';

export interface SubmitClaimRequest {
  type: ClaimType;
  market: Market;
  incidentDate: string;
  description: string;
  vehicleRegistration: string | null;
  propertyAddress: string | null;
  claimedAmountMinor: number;
}

// The API answers the claim's one open request, so no request id is sent.
// The version makes sure it is still the question the claimant saw.
export interface ProvideInformationRequest {
  expectedVersion: number;
  response: string;
}

export interface InformationRequest {
  id: number;
  question: string;
  status: InformationRequestStatus;
  requestedAtUtc: string;
  response: string | null;
  respondedAtUtc: string | null;
}

// The claimant view. It has no assessment or officer fields.
export interface ClaimantClaim {
  id: number;
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
  settlementAmountMinor: number | null;
  rejectionReason: string | null;
  submittedAtUtc: string;
  decidedAtUtc: string | null;
  version: number;
  informationRequests: InformationRequest[];
}

// The API leaves assessment events out of the claimant's history.
export type ClaimantHistoryAction =
  | 'Submitted'
  | 'PickedUp'
  | 'InformationRequested'
  | 'InformationProvided'
  | 'Settled'
  | 'Rejected';

export interface ClaimantHistoryEntry {
  action: ClaimantHistoryAction;
  fromStatus: ClaimStatus | null;
  toStatus: ClaimStatus;
  actorRole: 'Claimant' | 'ClaimsOfficer';
  occurredAtUtc: string;
  note: string | null;
  amountMinor: number | null;
  currencyCode: string | null;
}
