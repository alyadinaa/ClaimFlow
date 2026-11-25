using ClaimFlow.Api.Modules.Auth.Entities;

namespace ClaimFlow.Api.Modules.Claims.Entities;

// Append-only: rows are never updated or deleted by the application.
public class ClaimHistory
{
    private ClaimHistory()
    {
    }

    internal ClaimHistory(
        Claim claim,
        ClaimAction action,
        ClaimStatus? fromStatus,
        User actor,
        DateTime occurredAtUtc,
        string? note,
        long? amountMinor,
        InformationRequest? informationRequest)
    {
        Claim = claim;
        Action = action;
        FromStatus = fromStatus;
        ToStatus = claim.Status;
        ActorId = actor.Id;
        ActorRole = actor.Role;
        OccurredAtUtc = occurredAtUtc;
        Note = note;
        AmountMinor = amountMinor;
        CurrencyCode = amountMinor is null ? null : claim.CurrencyCode;
        // Linked by object, because a new request has no Id until it is saved.
        InformationRequest = informationRequest;
    }

    public int Id { get; private set; }

    public int ClaimId { get; private set; }

    public Claim Claim { get; private set; } = null!;

    public ClaimAction Action { get; private set; }

    // Null only for submission.
    public ClaimStatus? FromStatus { get; private set; }

    public ClaimStatus ToStatus { get; private set; }

    public int ActorId { get; private set; }

    public User Actor { get; private set; } = null!;

    // The actor's role at the time of the action.
    public UserRole ActorRole { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    public string? Note { get; private set; }

    public long? AmountMinor { get; private set; }

    public string? CurrencyCode { get; private set; }

    public int? InformationRequestId { get; private set; }

    public InformationRequest? InformationRequest { get; private set; }
}
