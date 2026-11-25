using System.ComponentModel.DataAnnotations;
using ClaimFlow.Api.Modules.Auth.Entities;
using ClaimFlow.Api.Modules.Claims.Entities;

namespace ClaimFlow.Api.Modules.Claims;

// Nullable with [Required], so a missing value is refused instead of becoming a default.
// Business rules are checked by Claim.Submit.
public record SubmitClaimRequest(
    [Required] ClaimType? Type,
    [Required] Market? Market,
    [Required] DateOnly? IncidentDate,
    [Required] string Description,
    string? VehicleRegistration,
    string? PropertyAddress,
    [Required] long? ClaimedAmountMinor);

public record PickUpClaimRequest([Required] int? ExpectedVersion);

// Only ExpectedVersion is required here. Other inputs are checked by Claim, after permission,
// version and status, so that the approved error order is kept.
public record RecordAssessmentRequest([Required] int? ExpectedVersion, long? AmountMinor);

public record RequestInformationRequest([Required] int? ExpectedVersion, string? Question);

public record ProvideInformationRequest([Required] int? ExpectedVersion, string? Response);

public record SettleClaimRequest([Required] int? ExpectedVersion, string? Note);

public record RejectClaimRequest([Required] int? ExpectedVersion, string? Reason);

// Shown to both roles. Contains no officer identity.
public record InformationRequestResponse(
    int Id,
    string Question,
    InformationRequestStatus Status,
    DateTime RequestedAtUtc,
    string? Response,
    DateTime? RespondedAtUtc)
{
    public static List<InformationRequestResponse> From(Claim c) => c.InformationRequests
        .OrderBy(r => r.Id)
        .Select(r => new InformationRequestResponse(
            r.Id, r.Question, r.Status, r.RequestedAtUtc, r.Response, r.RespondedAtUtc))
        .ToList();
}

// Claimant view: contains no assessment data and no officer names or IDs.
public record ClaimantClaimResponse(
    int Id,
    ClaimType Type,
    Market Market,
    string CurrencyCode,
    int CurrencyDecimalPlaces,
    ClaimStatus Status,
    DateOnly IncidentDate,
    string Description,
    string? VehicleRegistration,
    string? PropertyAddress,
    long ClaimedAmountMinor,
    long? SettlementAmountMinor,
    string? RejectionReason,
    DateTime SubmittedAtUtc,
    DateTime? DecidedAtUtc,
    int Version,
    List<InformationRequestResponse> InformationRequests)
{
    public static ClaimantClaimResponse From(Claim c) => new(
        c.Id, c.Type, c.Market, c.CurrencyCode, MarketCatalog.Get(c.Market).DecimalPlaces, c.Status,
        c.IncidentDate, c.Description, c.VehicleRegistration, c.PropertyAddress, c.ClaimedAmountMinor,
        c.SettlementAmountMinor, c.RejectionReason, c.SubmittedAtUtc, c.DecidedAtUtc, c.Version,
        InformationRequestResponse.From(c));
}

public record OfficerClaimResponse(
    int Id,
    int ClaimantId,
    string ClaimantName,
    int? AssignedOfficerId,
    string? AssignedOfficerName,
    ClaimType Type,
    Market Market,
    string CurrencyCode,
    int CurrencyDecimalPlaces,
    ClaimStatus Status,
    DateOnly IncidentDate,
    string Description,
    string? VehicleRegistration,
    string? PropertyAddress,
    long ClaimedAmountMinor,
    long? AssessedAmountMinor,
    long? SettlementAmountMinor,
    string? RejectionReason,
    DateTime SubmittedAtUtc,
    DateTime? DecidedAtUtc,
    int Version,
    List<InformationRequestResponse> InformationRequests)
{
    public static OfficerClaimResponse From(Claim c) => new(
        c.Id, c.ClaimantId, c.Claimant.DisplayName, c.AssignedOfficerId, c.AssignedOfficer?.DisplayName,
        c.Type, c.Market, c.CurrencyCode, MarketCatalog.Get(c.Market).DecimalPlaces, c.Status,
        c.IncidentDate, c.Description, c.VehicleRegistration, c.PropertyAddress, c.ClaimedAmountMinor,
        c.AssessedAmountMinor, c.SettlementAmountMinor, c.RejectionReason, c.SubmittedAtUtc, c.DecidedAtUtc, c.Version,
        InformationRequestResponse.From(c));
}

// Claimant view: assessment events are left out, and actors are shown by role only.
public record ClaimantHistoryResponse(
    ClaimAction Action,
    ClaimStatus? FromStatus,
    ClaimStatus ToStatus,
    UserRole ActorRole,
    DateTime OccurredAtUtc,
    string? Note,
    long? AmountMinor,
    string? CurrencyCode)
{
    public static ClaimantHistoryResponse From(ClaimHistory h) => new(
        h.Action, h.FromStatus, h.ToStatus, h.ActorRole, h.OccurredAtUtc, h.Note, h.AmountMinor, h.CurrencyCode);
}

public record OfficerHistoryResponse(
    int Id,
    ClaimAction Action,
    ClaimStatus? FromStatus,
    ClaimStatus ToStatus,
    int ActorId,
    string ActorName,
    UserRole ActorRole,
    DateTime OccurredAtUtc,
    string? Note,
    long? AmountMinor,
    string? CurrencyCode,
    int? InformationRequestId)
{
    public static OfficerHistoryResponse From(ClaimHistory h) => new(
        h.Id, h.Action, h.FromStatus, h.ToStatus, h.ActorId, h.Actor.DisplayName, h.ActorRole,
        h.OccurredAtUtc, h.Note, h.AmountMinor, h.CurrencyCode, h.InformationRequestId);
}
