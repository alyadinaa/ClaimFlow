using ClaimFlow.Api.Modules.Auth.Entities;
using ClaimFlow.Api.Modules.Claims;
using ClaimFlow.Api.Modules.Claims.Entities;
using static ClaimFlow.Tests.TestData;

namespace ClaimFlow.Tests;

public class ClaimLifecycleTests
{
    private readonly TestClock _clock = new();

    private static readonly DateOnly PastDate = new(2026, 11, 20);

    private Claim Submit(
        User? claimant = null,
        ClaimType type = ClaimType.Motor,
        Market market = Market.Singapore,
        DateOnly? incidentDate = null,
        string description = "Rear bumper damage",
        string? vehicle = "SGX1234A",
        string? address = null,
        long amount = 125_050) =>
        Claim.Submit(claimant ?? Claimant, type, market, incidentDate ?? PastDate,
            description, vehicle, address, amount, _clock);

    // T1

    [Fact]
    public void Submit_creates_unassigned_claim_with_history()
    {
        var claim = Submitted(_clock);

        Assert.Equal(ClaimStatus.Submitted, claim.Status);
        Assert.Equal(Claimant.Id, claim.ClaimantId);
        Assert.Null(claim.AssignedOfficerId);
        Assert.Equal("SGD", claim.CurrencyCode);
        Assert.Equal(1, claim.Version);
        Assert.Equal(_clock.Now.UtcDateTime, claim.SubmittedAtUtc);
        Assert.Equal(DateTimeKind.Utc, claim.SubmittedAtUtc.Kind);

        var history = Assert.Single(claim.History);
        Assert.Equal(ClaimAction.Submitted, history.Action);
        Assert.Null(history.FromStatus);
        Assert.Equal(ClaimStatus.Submitted, history.ToStatus);
        Assert.Equal(Claimant.Id, history.ActorId);
        Assert.Equal(UserRole.Claimant, history.ActorRole);
    }

    [Fact]
    public void Submit_accepts_property_claim()
    {
        var claim = Submit(type: ClaimType.Property, market: Market.Japan, vehicle: null, address: "1 Example Street");

        Assert.Equal(ClaimType.Property, claim.Type);
        Assert.Equal("JPY", claim.CurrencyCode);
    }

    [Fact]
    public void Officer_cannot_submit() =>
        AssertError(ClaimRuleError.Forbidden, () => Submit(claimant: Officer));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Submit_requires_description(string description) =>
        AssertError(ClaimRuleError.Validation, () => Submit(description: description));

    [Fact]
    public void Submit_limits_text_lengths()
    {
        AssertError(ClaimRuleError.Validation, () => Submit(description: new string('x', 2001)));
        AssertError(ClaimRuleError.Validation, () => Submit(vehicle: new string('x', 21)));
        AssertError(ClaimRuleError.Validation, () =>
            Submit(type: ClaimType.Property, vehicle: null, address: new string('x', 301)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(Claim.MaxAmountMinor + 1)]
    public void Submit_rejects_invalid_amount(long amount) =>
        AssertError(ClaimRuleError.Validation, () => Submit(amount: amount));

    [Fact]
    public void Submit_accepts_maximum_amount() =>
        Assert.Equal(Claim.MaxAmountMinor, Submit(amount: Claim.MaxAmountMinor).ClaimedAmountMinor);

    [Theory]
    [InlineData(ClaimType.Motor, null, null)]
    [InlineData(ClaimType.Motor, "SGX1234A", "1 Example Street")]
    [InlineData(ClaimType.Property, null, null)]
    [InlineData(ClaimType.Property, "SGX1234A", "1 Example Street")]
    public void Submit_requires_only_the_detail_for_its_type(ClaimType type, string? vehicle, string? address) =>
        AssertError(ClaimRuleError.Validation, () => Submit(type: type, vehicle: vehicle, address: address));

    [Fact]
    public void Submit_rejects_unsupported_type_and_market()
    {
        AssertError(ClaimRuleError.Validation, () => Submit(type: (ClaimType)99));
        AssertError(ClaimRuleError.Validation, () => Submit(market: (Market)99));
    }

    [Fact]
    public void Incident_date_uses_market_local_date()
    {
        // 25 Nov 20:00 UTC is already 26 Nov in Singapore.
        _clock.Now = new DateTimeOffset(2026, 11, 25, 20, 0, 0, TimeSpan.Zero);

        Assert.Equal(ClaimStatus.Submitted, Submit(incidentDate: new DateOnly(2026, 11, 26)).Status);
        AssertError(ClaimRuleError.Validation, () => Submit(incidentDate: new DateOnly(2026, 11, 27)));
    }

    // T2

    [Fact]
    public void PickUp_assigns_officer_and_starts_review()
    {
        var claim = Submitted(_clock);

        claim.PickUp(Officer, 1, _clock);

        Assert.Equal(ClaimStatus.InReview, claim.Status);
        Assert.Equal(Officer.Id, claim.AssignedOfficerId);
        Assert.Equal(2, claim.Version);
        AssertLastHistory(claim, ClaimAction.PickedUp, ClaimStatus.Submitted, ClaimStatus.InReview, Officer);
    }

    [Fact]
    public void Claimant_cannot_pick_up()
    {
        var claim = Submitted(_clock);
        AssertError(ClaimRuleError.Forbidden, () => claim.PickUp(Claimant, 1, _clock));
    }

    [Fact]
    public void Claim_cannot_be_picked_up_twice()
    {
        var claim = InReview(_clock);
        AssertError(ClaimRuleError.Conflict, () => claim.PickUp(OtherOfficer, claim.Version, _clock));
        Assert.Equal(Officer.Id, claim.AssignedOfficerId);
    }

    // T3

    [Fact]
    public void Assessment_keeps_status_and_records_amount()
    {
        var claim = InReview(_clock);

        claim.RecordAssessment(Officer, claim.Version, 200_000, _clock);

        Assert.Equal(ClaimStatus.InReview, claim.Status);
        Assert.Equal(200_000, claim.AssessedAmountMinor);
        Assert.Null(claim.SettlementAmountMinor);
        Assert.Equal(3, claim.Version);

        var history = AssertLastHistory(claim, ClaimAction.AssessmentRecorded, ClaimStatus.InReview, ClaimStatus.InReview, Officer);
        Assert.Equal(200_000, history.AmountMinor);
        Assert.Equal("SGD", history.CurrencyCode);
    }

    [Fact]
    public void Reassessment_with_same_amount_still_increments_version()
    {
        var claim = InReview(_clock);
        claim.RecordAssessment(Officer, claim.Version, 100, _clock);
        claim.RecordAssessment(Officer, claim.Version, 100, _clock);

        Assert.Equal(4, claim.Version);
        Assert.Equal(2, claim.History.Count(h => h.Action == ClaimAction.AssessmentRecorded));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(Claim.MaxAmountMinor + 1)]
    public void Assessment_rejects_invalid_amount(long amount)
    {
        var claim = InReview(_clock);
        AssertError(ClaimRuleError.Validation, () => claim.RecordAssessment(Officer, claim.Version, amount, _clock));
        AssertUnchanged(claim, ClaimStatus.InReview, 2);
    }

    // T4 and T5

    [Fact]
    public void Request_and_response_return_claim_to_review()
    {
        var claim = InReview(_clock);

        claim.RequestInformation(Officer, claim.Version, "Any photos?", _clock);

        Assert.Equal(ClaimStatus.AwaitingInformation, claim.Status);
        var request = Assert.Single(claim.InformationRequests);
        Assert.Equal(InformationRequestStatus.Open, request.Status);
        Assert.Equal(Officer.Id, request.RequestedById);
        var requested = AssertLastHistory(claim, ClaimAction.InformationRequested,
            ClaimStatus.InReview, ClaimStatus.AwaitingInformation, Officer);

        _clock.Now = _clock.Now.AddHours(1);
        claim.ProvideInformation(Claimant, claim.Version, "No photos.", _clock);

        Assert.Equal(ClaimStatus.InReview, claim.Status);
        Assert.Equal(Officer.Id, claim.AssignedOfficerId);
        Assert.Equal(InformationRequestStatus.Answered, request.Status);
        Assert.Equal("No photos.", request.Response);
        Assert.Equal(_clock.Now.UtcDateTime, request.RespondedAtUtc);
        Assert.Equal(4, claim.Version);
        var provided = AssertLastHistory(claim, ClaimAction.InformationProvided,
            ClaimStatus.AwaitingInformation, ClaimStatus.InReview, Claimant);

        // Both events refer to the same request.
        Assert.Same(request, requested.InformationRequest);
        Assert.Same(request, provided.InformationRequest);
    }

    [Fact]
    public void Several_rounds_are_allowed()
    {
        var claim = AwaitingInformation(_clock);
        claim.ProvideInformation(Claimant, claim.Version, "First answer", _clock);
        claim.RequestInformation(Officer, claim.Version, "Second question", _clock);

        Assert.Equal(2, claim.InformationRequests.Count);
        Assert.Single(claim.InformationRequests, r => r.Status == InformationRequestStatus.Open);
    }

    [Fact]
    public void Second_open_request_is_rejected()
    {
        var claim = AwaitingInformation(_clock);
        AssertError(ClaimRuleError.Conflict, () => claim.RequestInformation(Officer, claim.Version, "Again?", _clock));
        Assert.Single(claim.InformationRequests);
    }

    [Fact]
    public void Question_and_response_must_not_be_blank()
    {
        var inReview = InReview(_clock);
        AssertError(ClaimRuleError.Validation, () => inReview.RequestInformation(Officer, inReview.Version, " ", _clock));
        AssertUnchanged(inReview, ClaimStatus.InReview, 2);

        var awaiting = AwaitingInformation(_clock);
        AssertError(ClaimRuleError.Validation, () => awaiting.ProvideInformation(Claimant, awaiting.Version, " ", _clock));
        AssertUnchanged(awaiting, ClaimStatus.AwaitingInformation, 3);
    }

    [Fact]
    public void Only_the_owner_can_respond()
    {
        var claim = AwaitingInformation(_clock);
        AssertError(ClaimRuleError.NotFound, () => claim.ProvideInformation(OtherClaimant, claim.Version, "Answer", _clock));
        AssertError(ClaimRuleError.Forbidden, () => claim.ProvideInformation(Officer, claim.Version, "Answer", _clock));
        AssertUnchanged(claim, ClaimStatus.AwaitingInformation, 3);
    }

    [Fact]
    public void Response_without_open_request_is_rejected()
    {
        var claim = InReview(_clock);
        AssertError(ClaimRuleError.Conflict, () => claim.ProvideInformation(Claimant, claim.Version, "Answer", _clock));
    }

    // T6

    [Fact]
    public void Settlement_copies_latest_assessment()
    {
        var claim = InReview(_clock);
        claim.RecordAssessment(Officer, claim.Version, 50_000, _clock);
        claim.RecordAssessment(Officer, claim.Version, 80_000, _clock);
        _clock.Now = _clock.Now.AddDays(1);

        claim.Settle(Officer, claim.Version, "Approved in part.", _clock);

        Assert.Equal(ClaimStatus.Settled, claim.Status);
        Assert.Equal(80_000, claim.AssessedAmountMinor);
        Assert.Equal(80_000, claim.SettlementAmountMinor);
        Assert.Equal(_clock.Now.UtcDateTime, claim.DecidedAtUtc);
        Assert.Equal(Officer.Id, claim.AssignedOfficerId);

        var history = AssertLastHistory(claim, ClaimAction.Settled, ClaimStatus.InReview, ClaimStatus.Settled, Officer);
        Assert.Equal(80_000, history.AmountMinor);
        Assert.Equal("SGD", history.CurrencyCode);
        Assert.Equal("Approved in part.", history.Note);
    }

    [Fact]
    public void Settlement_requires_assessment()
    {
        var claim = InReview(_clock);
        AssertError(ClaimRuleError.Conflict, () => claim.Settle(Officer, claim.Version, null, _clock));
        Assert.Null(claim.SettlementAmountMinor);
        AssertUnchanged(claim, ClaimStatus.InReview, 2);
    }

    // T7 and T8

    [Fact]
    public void Reject_from_review_records_reason()
    {
        var claim = InReview(_clock);

        claim.Reject(Officer, claim.Version, "Not covered.", _clock);

        Assert.Equal(ClaimStatus.Rejected, claim.Status);
        Assert.Equal("Not covered.", claim.RejectionReason);
        Assert.Equal(_clock.Now.UtcDateTime, claim.DecidedAtUtc);
        var history = AssertLastHistory(claim, ClaimAction.Rejected, ClaimStatus.InReview, ClaimStatus.Rejected, Officer);
        Assert.Equal("Not covered.", history.Note);
        Assert.Null(history.InformationRequest);
    }

    [Fact]
    public void Reject_while_awaiting_information_closes_request()
    {
        var claim = AwaitingInformation(_clock);

        claim.Reject(Officer, claim.Version, "No reply.", _clock);

        Assert.Equal(ClaimStatus.Rejected, claim.Status);
        var request = Assert.Single(claim.InformationRequests);
        Assert.Equal(InformationRequestStatus.Closed, request.Status);
        Assert.Null(request.Response);
        Assert.Equal(4, claim.Version);

        var history = AssertLastHistory(claim, ClaimAction.Rejected,
            ClaimStatus.AwaitingInformation, ClaimStatus.Rejected, Officer);
        Assert.Same(request, history.InformationRequest);
        Assert.Equal(2, claim.History.Count(h => h.InformationRequest == request));
    }

    [Fact]
    public void Rejection_requires_reason_from_both_statuses()
    {
        var inReview = InReview(_clock);
        AssertError(ClaimRuleError.Validation, () => inReview.Reject(Officer, inReview.Version, "", _clock));

        var awaiting = AwaitingInformation(_clock);
        AssertError(ClaimRuleError.Validation, () => awaiting.Reject(Officer, awaiting.Version, "  ", _clock));
        AssertUnchanged(awaiting, ClaimStatus.AwaitingInformation, 3);
        Assert.Equal(InformationRequestStatus.Open, Assert.Single(awaiting.InformationRequests).Status);
    }

    // Rules shared by several transitions

    [Fact]
    public void Officer_actions_are_rejected_for_wrong_role_or_officer()
    {
        var claim = InReview(_clock);

        foreach (var user in new[] { Claimant, OtherOfficer })
        {
            AssertError(ClaimRuleError.Forbidden, () => claim.RecordAssessment(user, claim.Version, 100, _clock));
            AssertError(ClaimRuleError.Forbidden, () => claim.RequestInformation(user, claim.Version, "Question", _clock));
            AssertError(ClaimRuleError.Forbidden, () => claim.Settle(user, claim.Version, null, _clock));
            AssertError(ClaimRuleError.Forbidden, () => claim.Reject(user, claim.Version, "Reason", _clock));
        }

        AssertUnchanged(claim, ClaimStatus.InReview, 2);
    }

    [Fact]
    public void Officer_cannot_decide_before_pickup()
    {
        var claim = Submitted(_clock);
        AssertOfficerActionsConflict(claim);
        AssertUnchanged(claim, ClaimStatus.Submitted, 1);
    }

    [Fact]
    public void Officer_cannot_assess_or_settle_while_awaiting_information()
    {
        var claim = AwaitingInformation(_clock);
        AssertError(ClaimRuleError.Conflict, () => claim.RecordAssessment(Officer, claim.Version, 100, _clock));
        AssertError(ClaimRuleError.Conflict, () => claim.Settle(Officer, claim.Version, null, _clock));
        AssertUnchanged(claim, ClaimStatus.AwaitingInformation, 3);
    }

    [Fact]
    public void Terminal_claims_cannot_change()
    {
        foreach (var claim in new[] { Settled(_clock), Rejected(_clock) })
        {
            var version = claim.Version;
            var status = claim.Status;
            var historyCount = claim.History.Count;

            AssertOfficerActionsConflict(claim);
            AssertError(ClaimRuleError.Conflict, () => claim.PickUp(Officer, claim.Version, _clock));
            AssertError(ClaimRuleError.Conflict, () => claim.ProvideInformation(Claimant, claim.Version, "Answer", _clock));

            AssertUnchanged(claim, status, version);
            Assert.Equal(historyCount, claim.History.Count);
        }
    }

    [Fact]
    public void Stale_version_is_rejected_for_every_action()
    {
        var submitted = Submitted(_clock);
        AssertError(ClaimRuleError.Conflict, () => submitted.PickUp(Officer, 0, _clock));
        AssertUnchanged(submitted, ClaimStatus.Submitted, 1);

        var inReview = InReview(_clock);
        inReview.RecordAssessment(Officer, 2, 50_000, _clock);
        AssertError(ClaimRuleError.Conflict, () => inReview.RecordAssessment(Officer, 2, 80_000, _clock));
        AssertError(ClaimRuleError.Conflict, () => inReview.RequestInformation(Officer, 2, "Question", _clock));
        AssertError(ClaimRuleError.Conflict, () => inReview.Settle(Officer, 2, null, _clock));
        AssertError(ClaimRuleError.Conflict, () => inReview.Reject(Officer, 2, "Reason", _clock));
        AssertUnchanged(inReview, ClaimStatus.InReview, 3);
        Assert.Equal(50_000, inReview.AssessedAmountMinor);

        var awaiting = AwaitingInformation(_clock);
        AssertError(ClaimRuleError.Conflict, () => awaiting.ProvideInformation(Claimant, 2, "Answer", _clock));
        AssertError(ClaimRuleError.Conflict, () => awaiting.Reject(Officer, 2, "Reason", _clock));
        AssertUnchanged(awaiting, ClaimStatus.AwaitingInformation, 3);
    }

    [Fact]
    public void Full_lifecycle_records_every_action_in_order()
    {
        var claim = AwaitingInformation(_clock);
        claim.ProvideInformation(Claimant, claim.Version, "Answer", _clock);
        claim.RecordAssessment(Officer, claim.Version, 90_000, _clock);
        claim.Settle(Officer, claim.Version, null, _clock);

        Assert.Equal(
            [
                ClaimAction.Submitted, ClaimAction.PickedUp, ClaimAction.InformationRequested,
                ClaimAction.InformationProvided, ClaimAction.AssessmentRecorded, ClaimAction.Settled
            ],
            claim.History.Select(h => h.Action));
        Assert.Equal(6, claim.Version);
    }

    private void AssertOfficerActionsConflict(Claim claim)
    {
        AssertError(ClaimRuleError.Conflict, () => claim.RecordAssessment(Officer, claim.Version, 100, _clock));
        AssertError(ClaimRuleError.Conflict, () => claim.RequestInformation(Officer, claim.Version, "Question", _clock));
        AssertError(ClaimRuleError.Conflict, () => claim.Settle(Officer, claim.Version, null, _clock));
        AssertError(ClaimRuleError.Conflict, () => claim.Reject(Officer, claim.Version, "Reason", _clock));
    }

    // Every successful action adds one version and one history row, so the two counts match.
    private static void AssertUnchanged(Claim claim, ClaimStatus status, int version)
    {
        Assert.Equal(status, claim.Status);
        Assert.Equal(version, claim.Version);
        Assert.Equal(version, claim.History.Count);
    }

    private ClaimHistory AssertLastHistory(
        Claim claim, ClaimAction action, ClaimStatus from, ClaimStatus to, User actor)
    {
        var history = claim.History.Last();
        Assert.Equal(action, history.Action);
        Assert.Equal(from, history.FromStatus);
        Assert.Equal(to, history.ToStatus);
        Assert.Equal(actor.Id, history.ActorId);
        Assert.Equal(actor.Role, history.ActorRole);
        Assert.Equal(_clock.Now.UtcDateTime, history.OccurredAtUtc);
        return history;
    }
}
