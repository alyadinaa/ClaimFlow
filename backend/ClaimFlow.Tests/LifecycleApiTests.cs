using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClaimFlow.Api.Modules.Auth.Entities;

namespace ClaimFlow.Tests;

public sealed class LifecycleApiTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private HttpClient _claimant = null!;
    private HttpClient _officer = null!;
    private HttpClient _otherClaimant = null!;
    private HttpClient _otherOfficer = null!;

    public async Task InitializeAsync()
    {
        _claimant = await _api.LoginAsClaimantAsync();
        _officer = await _api.LoginAsOfficerAsync();
        _api.AddUser("other-claimant@demo.example", "Other-Password-1", UserRole.Claimant);
        _api.AddUser("other-officer@demo.example", "Other-Password-1", UserRole.ClaimsOfficer);
        _otherClaimant = await _api.LoginAsync("other-claimant@demo.example", "Other-Password-1");
        _otherOfficer = await _api.LoginAsync("other-officer@demo.example", "Other-Password-1");
    }

    public Task DisposeAsync()
    {
        _api.Dispose();
        return Task.CompletedTask;
    }

    private static Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<JsonElement>();

    private static Task<HttpResponseMessage> ActAsync(HttpClient officer, int id, string action, object body) =>
        officer.PostAsJsonAsync($"/api/officer/claims/{id}/{action}", body);

    private static Task<HttpResponseMessage> RespondAsync(HttpClient claimant, int id, int version, string response) =>
        claimant.PostAsJsonAsync($"/api/claims/{id}/information", new { expectedVersion = version, response });

    private static async Task<JsonElement> OkAsync(Task<HttpResponseMessage> request)
    {
        var response = await request;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await JsonAsync(response);
    }

    private static async Task AssertStatusAsync(HttpStatusCode expected, Task<HttpResponseMessage> request) =>
        Assert.Equal(expected, (await request).StatusCode);

    private static void AssertState(JsonElement claim, string status, int version)
    {
        Assert.Equal(status, claim.GetProperty("status").GetString());
        Assert.Equal(version, claim.GetProperty("version").GetInt32());
    }

    private Task<JsonElement> OfficerViewAsync(int id) =>
        _officer.GetFromJsonAsync<JsonElement>($"/api/officer/claims/{id}");

    // Version 1
    private async Task<int> SubmittedAsync()
    {
        var response = await _claimant.PostAsJsonAsync("/api/claims", new
        {
            type = "Motor",
            market = "Singapore",
            incidentDate = "2026-01-15",
            description = "Rear bumper damage",
            vehicleRegistration = "SGX1234A",
            claimedAmountMinor = 125_050
        });
        return (await JsonAsync(response)).GetProperty("id").GetInt32();
    }

    // Version 2
    private async Task<int> InReviewAsync()
    {
        var id = await SubmittedAsync();
        await OkAsync(ActAsync(_officer, id, "pickup", new { expectedVersion = 1 }));
        return id;
    }

    // Version 3
    private async Task<int> AwaitingInformationAsync()
    {
        var id = await InReviewAsync();
        await OkAsync(ActAsync(_officer, id, "information-requests", new { expectedVersion = 2, question = "Any photos?" }));
        return id;
    }

    // Assessment

    [Fact]
    public async Task Assigned_officer_can_record_assessment()
    {
        var id = await InReviewAsync();

        var claim = await OkAsync(ActAsync(_officer, id, "assessment", new { expectedVersion = 2, amountMinor = 98_765 }));

        AssertState(claim, "InReview", 3);
        Assert.Equal(98_765, claim.GetProperty("assessedAmountMinor").GetInt64());
        Assert.Equal(JsonValueKind.Null, claim.GetProperty("settlementAmountMinor").ValueKind);
    }

    [Theory]
    [InlineData("assessment")]
    [InlineData("information-requests")]
    [InlineData("settle")]
    [InlineData("reject")]
    public async Task Only_the_assigned_officer_can_act(string action)
    {
        var id = await InReviewAsync();
        var body = new { expectedVersion = 2, amountMinor = 100, question = "Question", reason = "Reason" };

        await AssertStatusAsync(HttpStatusCode.Forbidden, ActAsync(_otherOfficer, id, action, body));
        await AssertStatusAsync(HttpStatusCode.Forbidden, ActAsync(_claimant, id, action, body));
        await AssertStatusAsync(HttpStatusCode.Unauthorized, ActAsync(_api.CreateClient(), id, action, body));
        await AssertStatusAsync(HttpStatusCode.NotFound, ActAsync(_officer, 999, action, body));
        AssertState(await OfficerViewAsync(id), "InReview", 2);
    }

    [Fact]
    public async Task Invalid_input_returns_400()
    {
        var id = await InReviewAsync();

        await AssertStatusAsync(HttpStatusCode.BadRequest, ActAsync(_officer, id, "assessment", new { expectedVersion = 2, amountMinor = 0 }));
        await AssertStatusAsync(HttpStatusCode.BadRequest, ActAsync(_officer, id, "assessment", new { expectedVersion = 2 }));
        await AssertStatusAsync(HttpStatusCode.BadRequest, ActAsync(_officer, id, "information-requests", new { expectedVersion = 2, question = " " }));
        await AssertStatusAsync(HttpStatusCode.BadRequest, ActAsync(_officer, id, "reject", new { expectedVersion = 2 }));
        await AssertStatusAsync(HttpStatusCode.BadRequest, ActAsync(_officer, id, "settle", new { }));
        AssertState(await OfficerViewAsync(id), "InReview", 2);
    }

    // Each request is wrong in several ways; the approved order decides which error is returned.
    [Fact]
    public async Task Errors_follow_the_approved_order()
    {
        var id = await AwaitingInformationAsync();

        // Permission before version, status and input.
        await AssertStatusAsync(HttpStatusCode.Forbidden, ActAsync(_otherOfficer, id, "information-requests", new { expectedVersion = 1, question = "" }));
        await AssertStatusAsync(HttpStatusCode.NotFound, RespondAsync(_otherClaimant, id, 1, ""));

        // Version before status and input.
        var stale = await ActAsync(_officer, id, "information-requests", new { expectedVersion = 1, question = "" });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Contains("has changed", (await JsonAsync(stale)).GetProperty("detail").GetString());

        // Status before input.
        var wrongStatus = await ActAsync(_officer, id, "information-requests", new { expectedVersion = 3, question = "" });
        Assert.Equal(HttpStatusCode.Conflict, wrongStatus.StatusCode);
        Assert.Contains("AwaitingInformation", (await JsonAsync(wrongStatus)).GetProperty("detail").GetString());
    }

    // Information request and response

    [Fact]
    public async Task Officer_can_ask_and_claimant_can_respond()
    {
        var id = await InReviewAsync();
        var asked = await OkAsync(ActAsync(_officer, id, "information-requests", new { expectedVersion = 2, question = "Any photos?" }));
        AssertState(asked, "AwaitingInformation", 3);

        var claimantView = await _claimant.GetFromJsonAsync<JsonElement>($"/api/claims/{id}");
        var request = Assert.Single(claimantView.GetProperty("informationRequests").EnumerateArray());
        Assert.Equal("Any photos?", request.GetProperty("question").GetString());
        Assert.Equal("Open", request.GetProperty("status").GetString());

        var answered = await OkAsync(RespondAsync(_claimant, id, 3, "No photos."));

        AssertState(answered, "InReview", 4);
        var officerRequest = Assert.Single((await OfficerViewAsync(id)).GetProperty("informationRequests").EnumerateArray());
        Assert.Equal("Answered", officerRequest.GetProperty("status").GetString());
        Assert.Equal("No photos.", officerRequest.GetProperty("response").GetString());

        // Both history events refer to the same request.
        var history = (await _officer.GetFromJsonAsync<JsonElement>($"/api/officer/claims/{id}/history")).EnumerateArray().ToList();
        Assert.Equal(["Submitted", "PickedUp", "InformationRequested", "InformationProvided"],
            history.Select(h => h.GetProperty("action").GetString()!));
        Assert.All(history.Skip(2), h => Assert.Equal(
            request.GetProperty("id").GetInt32(), h.GetProperty("informationRequestId").GetInt32()));
    }

    [Fact]
    public async Task Only_the_owner_can_respond()
    {
        var id = await AwaitingInformationAsync();

        await AssertStatusAsync(HttpStatusCode.NotFound, RespondAsync(_otherClaimant, id, 3, "Answer"));
        await AssertStatusAsync(HttpStatusCode.NotFound, RespondAsync(_claimant, 999, 3, "Answer"));
        await AssertStatusAsync(HttpStatusCode.Forbidden, RespondAsync(_officer, id, 3, "Answer"));
        await AssertStatusAsync(HttpStatusCode.Unauthorized, RespondAsync(_api.CreateClient(), id, 3, "Answer"));
        await AssertStatusAsync(HttpStatusCode.BadRequest, RespondAsync(_claimant, id, 3, " "));
        AssertState(await OfficerViewAsync(id), "AwaitingInformation", 3);
    }

    [Fact]
    public async Task Response_without_open_request_returns_409()
    {
        var id = await InReviewAsync();
        await AssertStatusAsync(HttpStatusCode.Conflict, RespondAsync(_claimant, id, 2, "Answer"));
    }

    // Settlement

    [Fact]
    public async Task Settlement_requires_assessment()
    {
        var id = await InReviewAsync();
        await AssertStatusAsync(HttpStatusCode.Conflict, ActAsync(_officer, id, "settle", new { expectedVersion = 2 }));
        AssertState(await OfficerViewAsync(id), "InReview", 2);
    }

    [Fact]
    public async Task Settlement_returns_latest_assessed_amount()
    {
        var id = await InReviewAsync();
        await OkAsync(ActAsync(_officer, id, "assessment", new { expectedVersion = 2, amountMinor = 50_000 }));
        await OkAsync(ActAsync(_officer, id, "assessment", new { expectedVersion = 3, amountMinor = 80_000 }));

        var claim = await OkAsync(ActAsync(_officer, id, "settle", new { expectedVersion = 4, note = "Approved." }));

        AssertState(claim, "Settled", 5);
        Assert.Equal(80_000, claim.GetProperty("settlementAmountMinor").GetInt64());
        Assert.Equal(JsonValueKind.String, claim.GetProperty("decidedAtUtc").ValueKind);

        await AssertStatusAsync(HttpStatusCode.Conflict, ActAsync(_officer, id, "reject", new { expectedVersion = 5, reason = "Too late" }));
    }

    // The final assessment equals the settlement, so an earlier, different one is used to detect leaks.
    [Fact]
    public async Task Claimant_sees_settlement_but_never_assessment_of_a_settled_claim()
    {
        var id = await InReviewAsync();
        await OkAsync(ActAsync(_officer, id, "assessment", new { expectedVersion = 2, amountMinor = 54_321 }));
        await OkAsync(ActAsync(_officer, id, "assessment", new { expectedVersion = 3, amountMinor = 80_000 }));
        await OkAsync(ActAsync(_officer, id, "settle", new { expectedVersion = 4, note = "Approved." }));

        foreach (var url in new[] { "/api/claims", $"/api/claims/{id}", $"/api/claims/{id}/history" })
        {
            var text = await _claimant.GetStringAsync(url);
            Assert.DoesNotContain("assess", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("54321", text);
            Assert.DoesNotContain("Demo Officer", text);
        }

        var claim = await _claimant.GetFromJsonAsync<JsonElement>($"/api/claims/{id}");
        AssertState(claim, "Settled", 5);
        Assert.Equal(80_000, claim.GetProperty("settlementAmountMinor").GetInt64());
        Assert.False(claim.TryGetProperty("assessedAmountMinor", out _));

        var history = (await _claimant.GetFromJsonAsync<JsonElement>($"/api/claims/{id}/history")).EnumerateArray().ToList();
        Assert.Equal(["Submitted", "PickedUp", "Settled"], history.Select(h => h.GetProperty("action").GetString()!));
        Assert.Equal(80_000, history[2].GetProperty("amountMinor").GetInt64());
        Assert.Equal("SGD", history[2].GetProperty("currencyCode").GetString());
    }

    [Fact]
    public async Task Claimant_response_to_a_request_contains_no_assessment()
    {
        var id = await InReviewAsync();
        await OkAsync(ActAsync(_officer, id, "assessment", new { expectedVersion = 2, amountMinor = 54_321 }));
        await OkAsync(ActAsync(_officer, id, "information-requests", new { expectedVersion = 3, question = "Any photos?" }));

        var text = await (await RespondAsync(_claimant, id, 4, "No photos.")).Content.ReadAsStringAsync();

        Assert.Contains("No photos.", text);
        Assert.DoesNotContain("assess", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("54321", text);
    }

    // Rejection

    [Fact]
    public async Task Rejection_from_review_shows_reason_to_claimant()
    {
        var id = await InReviewAsync();

        var claim = await OkAsync(ActAsync(_officer, id, "reject", new { expectedVersion = 2, reason = "Not covered." }));

        AssertState(claim, "Rejected", 3);
        var claimantView = await _claimant.GetFromJsonAsync<JsonElement>($"/api/claims/{id}");
        Assert.Equal("Rejected", claimantView.GetProperty("status").GetString());
        Assert.Equal("Not covered.", claimantView.GetProperty("rejectionReason").GetString());
    }

    [Fact]
    public async Task T8_rejection_closes_request_and_persists_history()
    {
        var id = await AwaitingInformationAsync();

        var claim = await OkAsync(ActAsync(_officer, id, "reject", new { expectedVersion = 3, reason = "No reply." }));

        AssertState(claim, "Rejected", 4);
        var request = Assert.Single((await OfficerViewAsync(id)).GetProperty("informationRequests").EnumerateArray());
        Assert.Equal("Closed", request.GetProperty("status").GetString());

        var last = (await _officer.GetFromJsonAsync<JsonElement>($"/api/officer/claims/{id}/history")).EnumerateArray().Last();
        Assert.Equal("Rejected", last.GetProperty("action").GetString());
        Assert.Equal("AwaitingInformation", last.GetProperty("fromStatus").GetString());
        Assert.Equal("No reply.", last.GetProperty("note").GetString());
        Assert.Equal(request.GetProperty("id").GetInt32(), last.GetProperty("informationRequestId").GetInt32());

        await AssertStatusAsync(HttpStatusCode.Conflict, RespondAsync(_claimant, id, 4, "Too late"));
    }

    // Versions

    [Fact]
    public async Task Stale_versions_return_409_for_every_action()
    {
        var id = await InReviewAsync();
        await OkAsync(ActAsync(_officer, id, "assessment", new { expectedVersion = 2, amountMinor = 50_000 }));

        await AssertStatusAsync(HttpStatusCode.Conflict, ActAsync(_officer, id, "assessment", new { expectedVersion = 2, amountMinor = 80_000 }));
        await AssertStatusAsync(HttpStatusCode.Conflict, ActAsync(_officer, id, "information-requests", new { expectedVersion = 2, question = "Question" }));
        await AssertStatusAsync(HttpStatusCode.Conflict, ActAsync(_officer, id, "settle", new { expectedVersion = 2 }));
        await AssertStatusAsync(HttpStatusCode.Conflict, ActAsync(_officer, id, "reject", new { expectedVersion = 2, reason = "Reason" }));

        var claim = await OfficerViewAsync(id);
        AssertState(claim, "InReview", 3);
        Assert.Equal(50_000, claim.GetProperty("assessedAmountMinor").GetInt64());

        var awaitingId = await AwaitingInformationAsync();
        await AssertStatusAsync(HttpStatusCode.Conflict, RespondAsync(_claimant, awaitingId, 2, "Answer"));
        AssertState(await OfficerViewAsync(awaitingId), "AwaitingInformation", 3);
    }
}
