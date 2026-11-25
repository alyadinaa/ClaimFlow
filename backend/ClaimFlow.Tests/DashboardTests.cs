using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClaimFlow.Api.Data;
using ClaimFlow.Api.Modules.Auth.Entities;
using ClaimFlow.Api.Modules.Claims.Entities;

namespace ClaimFlow.Tests;

public sealed class DashboardTests : IDisposable
{
    private const string OtherOfficerEmail = "other-officer@demo.example";

    private readonly ApiFactory _api = new();
    private readonly TestClock _clock = new();

    public DashboardTests() => _api.AddUser(OtherOfficerEmail, "Other-Password-1", UserRole.ClaimsOfficer, "Other Officer");

    public void Dispose() => _api.Dispose();

    // Creates a claim and lets the test move it on, using the real lifecycle methods.
    private void AddClaim(
        Market market = Market.Singapore,
        long claimed = 100_000,
        string officerEmail = ApiFactory.OfficerEmail,
        Action<Claim, User, User>? then = null) =>
        _api.WithDb(db =>
        {
            var claimant = db.Users.Single(u => u.Email == ApiFactory.ClaimantEmail);
            var officer = db.Users.Single(u => u.Email == officerEmail);
            var claim = Claim.Submit(claimant, ClaimType.Motor, market, new DateOnly(2026, 11, 1),
                "Damage", "ABC123", null, claimed, _clock);
            then?.Invoke(claim, officer, claimant);
            db.Claims.Add(claim);
            return db.SaveChanges();
        });

    private void PickUp(Claim claim, User officer, User claimant) => claim.PickUp(officer, claim.Version, _clock);

    private Action<Claim, User, User> Assess(long amount) => (claim, officer, claimant) =>
    {
        PickUp(claim, officer, claimant);
        claim.RecordAssessment(officer, claim.Version, amount, _clock);
    };

    private void Ask(Claim claim, User officer, User claimant)
    {
        PickUp(claim, officer, claimant);
        claim.RequestInformation(officer, claim.Version, "Question", _clock);
    }

    private Action<Claim, User, User> Settle(long amount, TimeSpan after) => (claim, officer, claimant) =>
    {
        Assess(amount)(claim, officer, claimant);
        Later(after, () => claim.Settle(officer, claim.Version, null, _clock));
    };

    private Action<Claim, User, User> Reject(TimeSpan after) => (claim, officer, claimant) =>
    {
        PickUp(claim, officer, claimant);
        Later(after, () => claim.Reject(officer, claim.Version, "Reason", _clock));
    };

    private void Later(TimeSpan after, Action action)
    {
        var start = _clock.Now;
        _clock.Now = start + after;
        action();
        _clock.Now = start;
    }

    private async Task<JsonElement> GetAsync(string path = "")
    {
        var officer = await _api.LoginAsOfficerAsync();
        return await officer.GetFromJsonAsync<JsonElement>("/api/officer/dashboard" + path);
    }

    private static int Int(JsonElement element, string name) => element.GetProperty(name).GetInt32();

    private static JsonElement Officer(JsonElement dashboard, string name) => dashboard.GetProperty("officers")
        .EnumerateArray().Single(o => o.GetProperty("officerName").GetString() == name);

    [Theory]
    [InlineData("/api/officer/dashboard")]
    [InlineData("/api/officer/dashboard/exposure")]
    public async Task Dashboard_needs_an_officer(string url)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _api.CreateClient().GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await (await _api.LoginAsClaimantAsync()).GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task Empty_dashboard_lists_every_status_and_officer_with_zeros()
    {
        var dashboard = await GetAsync();

        Assert.Equal(0, Int(dashboard, "totalClaims"));
        Assert.Equal(["Submitted", "InReview", "AwaitingInformation", "Settled", "Rejected"],
            dashboard.GetProperty("claimsByStatus").EnumerateArray().Select(s => s.GetProperty("status").GetString()!));
        Assert.Equal(2, dashboard.GetProperty("officers").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, Officer(dashboard, "Demo Officer").GetProperty("averageSecondsToDecision").ValueKind);
        Assert.Empty((await GetAsync("/exposure")).EnumerateArray());
    }

    [Fact]
    public async Task Counts_claims_by_status_and_officer()
    {
        AddClaim();
        AddClaim();
        AddClaim(then: PickUp);
        AddClaim(then: Ask);
        AddClaim(then: Settle(1, TimeSpan.FromHours(1)));
        AddClaim(then: Reject(TimeSpan.FromHours(1)));
        AddClaim(officerEmail: OtherOfficerEmail, then: PickUp);

        var dashboard = await GetAsync();

        Assert.Equal(7, Int(dashboard, "totalClaims"));
        Assert.Equal(2, Int(dashboard, "unassignedClaims"));
        Assert.Equal(5, Int(dashboard, "openClaims"));
        Assert.Equal(2, Int(dashboard, "decidedClaims"));
        Assert.Equal([2, 2, 1, 1, 1],
            dashboard.GetProperty("claimsByStatus").EnumerateArray().Select(s => Int(s, "count")));

        var demo = Officer(dashboard, "Demo Officer");
        Assert.Equal(2, Int(demo, "openAssigned"));
        Assert.Equal(1, Int(demo, "awaitingInformation"));
        Assert.Equal(2, Int(demo, "decided"));

        var other = Officer(dashboard, "Other Officer");
        Assert.Equal(1, Int(other, "openAssigned"));
        Assert.Equal(0, Int(other, "awaitingInformation"));
        Assert.Equal(0, Int(other, "decided"));
    }

    [Fact]
    public async Task Averages_submission_to_decision_per_officer()
    {
        AddClaim(then: Settle(1, TimeSpan.FromHours(2)));
        AddClaim(then: Reject(TimeSpan.FromHours(5)));
        AddClaim(then: PickUp);
        AddClaim(officerEmail: OtherOfficerEmail, then: Reject(TimeSpan.FromDays(3)));

        var dashboard = await GetAsync();

        var demo = Officer(dashboard, "Demo Officer");
        Assert.Equal(2, Int(demo, "decided"));
        Assert.Equal(TimeSpan.FromHours(3.5).TotalSeconds, demo.GetProperty("averageSecondsToDecision").GetDouble(), 3);
        var other = Officer(dashboard, "Other Officer");
        Assert.Equal(1, Int(other, "decided"));
        Assert.Equal(TimeSpan.FromDays(3).TotalSeconds, other.GetProperty("averageSecondsToDecision").GetDouble(), 3);
    }

    [Fact]
    public async Task Average_includes_time_waiting_for_the_claimant()
    {
        AddClaim(then: (claim, officer, claimant) =>
        {
            Ask(claim, officer, claimant);
            Later(TimeSpan.FromHours(10), () => claim.ProvideInformation(claimant, claim.Version, "Answer", _clock));
            Later(TimeSpan.FromHours(11), () => claim.Reject(officer, claim.Version, "Reason", _clock));
        });

        var demo = Officer(await GetAsync(), "Demo Officer");
        Assert.Equal(TimeSpan.FromHours(11).TotalSeconds, demo.GetProperty("averageSecondsToDecision").GetDouble(), 3);
    }

    [Fact]
    public async Task Exposure_uses_assessed_amount_or_falls_back_to_claimed()
    {
        AddClaim(claimed: 100_000);
        AddClaim(claimed: 50_000, then: Assess(30_000));
        AddClaim(claimed: 1_000, then: Assess(4_000));
        AddClaim(claimed: 500, then: Ask);

        var row = Assert.Single((await GetAsync("/exposure")).EnumerateArray());

        Assert.Equal("SGD", row.GetProperty("currencyCode").GetString());
        Assert.Equal(4, Int(row, "openClaims"));
        Assert.Equal(100_000 + 30_000 + 4_000 + 500, row.GetProperty("exposureMinor").GetInt64());
    }

    [Fact]
    public async Task Exposure_keeps_currencies_separate()
    {
        AddClaim(Market.Japan, 10_000);
        AddClaim(Market.Japan, 5_000);
        AddClaim(Market.Malaysia, 700);
        AddClaim(Market.Singapore, Claim.MaxAmountMinor);

        var rows = (await GetAsync("/exposure")).EnumerateArray().ToList();

        Assert.Equal(["JPY", "MYR", "SGD"], rows.Select(r => r.GetProperty("currencyCode").GetString()!));
        Assert.Equal([0, 2, 2], rows.Select(r => Int(r, "currencyDecimalPlaces")));
        Assert.Equal([2, 1, 1], rows.Select(r => Int(r, "openClaims")));
        Assert.Equal([15_000, 700, Claim.MaxAmountMinor], rows.Select(r => r.GetProperty("exposureMinor").GetInt64()));
    }

    [Fact]
    public async Task Exposure_excludes_settled_and_rejected_claims()
    {
        AddClaim(claimed: 100);
        AddClaim(claimed: 1_000, then: Settle(900, TimeSpan.FromHours(1)));
        AddClaim(claimed: 10_000, then: Reject(TimeSpan.FromHours(1)));
        AddClaim(Market.Japan, 5_000, then: Reject(TimeSpan.FromHours(1)));

        var row = Assert.Single((await GetAsync("/exposure")).EnumerateArray());

        Assert.Equal("SGD", row.GetProperty("currencyCode").GetString());
        Assert.Equal(1, Int(row, "openClaims"));
        Assert.Equal(100, row.GetProperty("exposureMinor").GetInt64());
    }

    [Fact]
    public async Task Dashboard_contains_no_sensitive_user_data()
    {
        AddClaim(then: PickUp);

        var text = (await GetAsync()).ToString() + await GetAsync("/exposure");

        Assert.DoesNotContain("hash", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@", text);
        Assert.DoesNotContain("Demo Claimant", text);
    }
}
