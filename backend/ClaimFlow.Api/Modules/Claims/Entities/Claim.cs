using ClaimFlow.Api.Modules.Auth.Entities;

namespace ClaimFlow.Api.Modules.Claims.Entities;

public class Claim
{
    // Technical safeguard (developer assumption), not a rule from the assessment.
    public const long MaxAmountMinor = 1_000_000_000_000;

    private const int MaxTextLength = 2000;
    private const int MaxVehicleRegistrationLength = 20;
    private const int MaxPropertyAddressLength = 300;

    private readonly List<ClaimHistory> _history = [];
    private readonly List<InformationRequest> _informationRequests = [];

    private Claim()
    {
    }

    public int Id { get; private set; }

    public int ClaimantId { get; private set; }

    public User Claimant { get; private set; } = null!;

    // Null only while the claim is Submitted.
    public int? AssignedOfficerId { get; private set; }

    public User? AssignedOfficer { get; private set; }

    public ClaimType Type { get; private set; }

    public Market Market { get; private set; }

    public string CurrencyCode { get; private set; } = null!;

    public ClaimStatus Status { get; private set; }

    public DateOnly IncidentDate { get; private set; }

    public string Description { get; private set; } = null!;

    // Motor claims only.
    public string? VehicleRegistration { get; private set; }

    // Property claims only.
    public string? PropertyAddress { get; private set; }

    // Amounts are whole numbers of the currency's smallest unit.
    public long ClaimedAmountMinor { get; private set; }

    public long? AssessedAmountMinor { get; private set; }

    public long? SettlementAmountMinor { get; private set; }

    public string? RejectionReason { get; private set; }

    public DateTime SubmittedAtUtc { get; private set; }

    public DateTime? DecidedAtUtc { get; private set; }

    public int Version { get; private set; } = 1;

    public IReadOnlyCollection<ClaimHistory> History => _history.AsReadOnly();

    public IReadOnlyCollection<InformationRequest> InformationRequests => _informationRequests.AsReadOnly();

    // T1
    public static Claim Submit(
        User claimant,
        ClaimType type,
        Market market,
        DateOnly incidentDate,
        string description,
        string? vehicleRegistration,
        string? propertyAddress,
        long claimedAmountMinor,
        TimeProvider clock)
    {
        RequireRole(claimant, UserRole.Claimant);

        var now = clock.GetUtcNow();
        var marketInfo = MarketCatalog.Get(market);

        Validate(Enum.IsDefined(type), "Claim type is not supported.");
        Validate(incidentDate <= MarketCatalog.LocalDate(market, now), "Incident date cannot be in the future.");
        RequireText(description, "Description", MaxTextLength);
        RequireAmount(claimedAmountMinor, "Claimed amount");

        if (type == ClaimType.Motor)
        {
            RequireText(vehicleRegistration, "Vehicle registration", MaxVehicleRegistrationLength);
            Validate(propertyAddress is null, "Property address is not allowed for a motor claim.");
        }
        else
        {
            RequireText(propertyAddress, "Property address", MaxPropertyAddressLength);
            Validate(vehicleRegistration is null, "Vehicle registration is not allowed for a property claim.");
        }

        var claim = new Claim
        {
            ClaimantId = claimant.Id,
            Type = type,
            Market = market,
            CurrencyCode = marketInfo.CurrencyCode,
            Status = ClaimStatus.Submitted,
            IncidentDate = incidentDate,
            Description = description,
            VehicleRegistration = vehicleRegistration,
            PropertyAddress = propertyAddress,
            ClaimedAmountMinor = claimedAmountMinor,
            SubmittedAtUtc = now.UtcDateTime
        };

        claim.AddHistory(ClaimAction.Submitted, null, claimant, now.UtcDateTime);
        return claim;
    }

    // T2
    public void PickUp(User officer, int expectedVersion, TimeProvider clock)
    {
        RequireRole(officer, UserRole.ClaimsOfficer);
        RequireVersion(expectedVersion);
        RequireStatus(ClaimStatus.Submitted);

        AssignedOfficerId = officer.Id;
        ChangeStatus(ClaimStatus.InReview, ClaimAction.PickedUp, officer, clock);
    }

    // T3
    public void RecordAssessment(User officer, int expectedVersion, long amountMinor, TimeProvider clock)
    {
        RequireAssignedOfficer(officer);
        RequireVersion(expectedVersion);
        RequireStatus(ClaimStatus.InReview);
        RequireAmount(amountMinor, "Assessed amount");

        AssessedAmountMinor = amountMinor;
        ChangeStatus(ClaimStatus.InReview, ClaimAction.AssessmentRecorded, officer, clock, amountMinor: amountMinor);
    }

    // T4
    public void RequestInformation(User officer, int expectedVersion, string question, TimeProvider clock)
    {
        RequireAssignedOfficer(officer);
        RequireVersion(expectedVersion);
        RequireStatus(ClaimStatus.InReview);
        RequireText(question, "Question", MaxTextLength);

        var request = new InformationRequest(this, officer, question, clock.GetUtcNow().UtcDateTime);
        _informationRequests.Add(request);
        ChangeStatus(ClaimStatus.AwaitingInformation, ClaimAction.InformationRequested, officer, clock, request: request);
    }

    // T5
    public void ProvideInformation(User claimant, int expectedVersion, string response, TimeProvider clock)
    {
        RequireRole(claimant, UserRole.Claimant);
        if (claimant.Id != ClaimantId)
        {
            throw new ClaimRuleException(ClaimRuleError.NotFound, "Claim was not found.");
        }

        RequireVersion(expectedVersion);
        RequireStatus(ClaimStatus.AwaitingInformation);
        RequireText(response, "Response", MaxTextLength);

        var request = OpenRequest();
        request.Answer(response, clock.GetUtcNow().UtcDateTime);
        ChangeStatus(ClaimStatus.InReview, ClaimAction.InformationProvided, claimant, clock, request: request);
    }

    // T6
    public void Settle(User officer, int expectedVersion, string? note, TimeProvider clock)
    {
        RequireAssignedOfficer(officer);
        RequireVersion(expectedVersion);
        RequireStatus(ClaimStatus.InReview);
        Validate(note is null || note.Length <= MaxTextLength, $"Note must be at most {MaxTextLength} characters.");

        if (AssessedAmountMinor is null)
        {
            throw new ClaimRuleException(ClaimRuleError.Conflict, "The claim must be assessed before settlement.");
        }

        SettlementAmountMinor = AssessedAmountMinor;
        ChangeStatus(ClaimStatus.Settled, ClaimAction.Settled, officer, clock,
            note: note, amountMinor: SettlementAmountMinor);
        DecidedAtUtc = _history[^1].OccurredAtUtc;
    }

    // T7 (from InReview) and T8 (from AwaitingInformation)
    public void Reject(User officer, int expectedVersion, string reason, TimeProvider clock)
    {
        RequireAssignedOfficer(officer);
        RequireVersion(expectedVersion);
        RequireStatus(ClaimStatus.InReview, ClaimStatus.AwaitingInformation);
        RequireText(reason, "Rejection reason", MaxTextLength);

        InformationRequest? request = null;
        if (Status == ClaimStatus.AwaitingInformation)
        {
            request = OpenRequest();
            request.Close();
        }

        RejectionReason = reason;
        ChangeStatus(ClaimStatus.Rejected, ClaimAction.Rejected, officer, clock, note: reason, request: request);
        DecidedAtUtc = _history[^1].OccurredAtUtc;
    }

    private void ChangeStatus(
        ClaimStatus newStatus,
        ClaimAction action,
        User actor,
        TimeProvider clock,
        string? note = null,
        long? amountMinor = null,
        InformationRequest? request = null)
    {
        var fromStatus = Status;
        Status = newStatus;
        Version++;
        AddHistory(action, fromStatus, actor, clock.GetUtcNow().UtcDateTime, note, amountMinor, request);
    }

    private void AddHistory(
        ClaimAction action,
        ClaimStatus? fromStatus,
        User actor,
        DateTime occurredAtUtc,
        string? note = null,
        long? amountMinor = null,
        InformationRequest? request = null)
    {
        _history.Add(new ClaimHistory(this, action, fromStatus, actor, occurredAtUtc, note, amountMinor, request));
    }

    // AwaitingInformation always has one open request, so a missing one means it was not loaded.
    private InformationRequest OpenRequest() =>
        _informationRequests.SingleOrDefault(r => r.Status == InformationRequestStatus.Open)
        ?? throw new InvalidOperationException("Information requests must be loaded with the claim.");

    private void RequireAssignedOfficer(User officer)
    {
        RequireRole(officer, UserRole.ClaimsOfficer);

        // An unassigned claim is Submitted, so the status check reports the conflict.
        if (AssignedOfficerId is not null && AssignedOfficerId != officer.Id)
        {
            throw new ClaimRuleException(ClaimRuleError.Forbidden, "The claim is assigned to another officer.");
        }
    }

    private void RequireVersion(int expectedVersion)
    {
        if (expectedVersion != Version)
        {
            throw new ClaimRuleException(ClaimRuleError.Conflict, "The claim has changed. Reload it and try again.");
        }
    }

    private void RequireStatus(params ClaimStatus[] allowed)
    {
        if (!allowed.Contains(Status))
        {
            throw new ClaimRuleException(ClaimRuleError.Conflict, $"This action is not allowed while the claim is {Status}.");
        }
    }

    private static void RequireRole(User user, UserRole role)
    {
        if (user.Role != role)
        {
            throw new ClaimRuleException(ClaimRuleError.Forbidden, "Your role cannot perform this action.");
        }
    }

    private static void RequireAmount(long amountMinor, string name) =>
        Validate(amountMinor is > 0 and <= MaxAmountMinor, $"{name} must be between 1 and {MaxAmountMinor} minor units.");

    private static void RequireText(string? value, string name, int maxLength)
    {
        Validate(!string.IsNullOrWhiteSpace(value), $"{name} is required.");
        Validate(value!.Length <= maxLength, $"{name} must be at most {maxLength} characters.");
    }

    private static void Validate(bool isValid, string message)
    {
        if (!isValid)
        {
            throw new ClaimRuleException(ClaimRuleError.Validation, message);
        }
    }
}
