using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClaimFlow.Api.Data;
using ClaimFlow.Api.Modules.Auth.Entities;
using ClaimFlow.Api.Modules.Claims.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ClaimFlow.Tests;

public sealed class ApiTests : IDisposable
{
    private readonly ApiFactory _api = new();

    public void Dispose() => _api.Dispose();

    private static object MotorClaim(string incidentDate = "2026-01-15", long amount = 125_050) => new
    {
        type = "Motor",
        market = "Singapore",
        incidentDate,
        description = "Rear bumper damage",
        vehicleRegistration = "SGX1234A",
        claimedAmountMinor = amount
    };

    private static async Task<int> SubmitAsync(HttpClient claimant)
    {
        var response = await claimant.PostAsJsonAsync("/api/claims", MotorClaim());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await JsonAsync(response)).GetProperty("id").GetInt32();
    }

    private static Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<JsonElement>();

    private static Task<HttpResponseMessage> PickUpAsync(HttpClient officer, int id, int version) =>
        officer.PostAsJsonAsync($"/api/officer/claims/{id}/pickup", new { expectedVersion = version });

    private async Task<HttpClient> OtherAsync(UserRole role)
    {
        _api.AddUser($"other-{role}@demo.example", "Other-Password-1", role);
        return await _api.LoginAsync($"other-{role}@demo.example", "Other-Password-1");
    }

    // Login

    [Fact]
    public async Task Login_returns_token_with_user_id_and_role_only()
    {
        var response = await _api.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { email = "CLAIMANT@demo.example", password = ApiFactory.ClaimantPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("hash", text, StringComparison.OrdinalIgnoreCase);

        var body = JsonSerializer.Deserialize<JsonElement>(text);
        Assert.Equal("Claimant", body.GetProperty("role").GetString());

        var token = new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(body.GetProperty("accessToken").GetString());
        var userId = _api.WithDb(db => db.Users.Single(u => u.Email == ApiFactory.ClaimantEmail).Id);
        Assert.Equal(userId.ToString(), token.GetClaim("sub").Value);
        Assert.Equal("Claimant", token.GetClaim("role").Value);
    }

    [Theory]
    [InlineData(ApiFactory.ClaimantEmail, "wrong")]
    [InlineData("nobody@demo.example", ApiFactory.ClaimantPassword)]
    public async Task Invalid_login_is_rejected_the_same_way(string email, string password)
    {
        var response = await _api.CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Email or password is incorrect.", (await JsonAsync(response)).GetProperty("detail").GetString());
    }

    // Access

    [Theory]
    [InlineData("GET", "/api/claims")]
    [InlineData("POST", "/api/claims")]
    [InlineData("GET", "/api/claims/1")]
    [InlineData("GET", "/api/claims/1/history")]
    [InlineData("GET", "/api/officer/claims")]
    [InlineData("GET", "/api/officer/claims/1")]
    [InlineData("GET", "/api/officer/claims/1/history")]
    [InlineData("POST", "/api/officer/claims/1/pickup")]
    public async Task Requests_need_a_token_with_the_right_role(string method, string url)
    {
        var wrongRole = url.Contains("officer") ? await _api.LoginAsClaimantAsync() : await _api.LoginAsOfficerAsync();
        var anonymous = _api.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(anonymous, method, url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(wrongRole, method, url)).StatusCode);
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string url)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method == "POST")
        {
            request.Content = JsonContent.Create(new { });
        }

        return client.SendAsync(request);
    }

    [Fact]
    public async Task Modified_token_is_rejected()
    {
        var client = await _api.LoginAsClaimantAsync();
        var parts = client.DefaultRequestHeaders.Authorization!.Parameter!.Split('.');
        // Change only the role, so the signature is the only reason to reject the token.
        var original = Base64UrlEncoder.Decode(parts[1]);
        var payload = Base64UrlEncoder.Encode(original.Replace("\"Claimant\"", "\"ClaimsOfficer\""));
        Assert.NotEqual(parts[1], payload);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", $"{parts[0]}.{payload}.{parts[2]}");

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/officer/claims")).StatusCode);
    }

    // Claimant

    [Fact]
    public async Task Claimant_can_submit_and_retrieve_own_claim()
    {
        var claimant = await _api.LoginAsClaimantAsync();

        var created = await claimant.PostAsJsonAsync("/api/claims", MotorClaim());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await JsonAsync(created)).GetProperty("id").GetInt32();
        Assert.EndsWith($"/api/claims/{id}", created.Headers.Location!.ToString());

        var claim = await claimant.GetFromJsonAsync<JsonElement>($"/api/claims/{id}");
        Assert.Equal("Submitted", claim.GetProperty("status").GetString());
        Assert.Equal("SGD", claim.GetProperty("currencyCode").GetString());
        Assert.Equal(2, claim.GetProperty("currencyDecimalPlaces").GetInt32());
        Assert.Equal(125_050, claim.GetProperty("claimedAmountMinor").GetInt64());
        Assert.Equal(1, claim.GetProperty("version").GetInt32());
        Assert.EndsWith("Z", claim.GetProperty("submittedAtUtc").GetString());

        var list = await claimant.GetFromJsonAsync<JsonElement>("/api/claims");
        Assert.Equal(id, Assert.Single(list.EnumerateArray()).GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task Invalid_claim_returns_400()
    {
        var claimant = await _api.LoginAsClaimantAsync();

        var futureDate = await claimant.PostAsJsonAsync("/api/claims", MotorClaim(incidentDate: "2999-01-01"));
        Assert.Equal(HttpStatusCode.BadRequest, futureDate.StatusCode);
        Assert.Equal("Incident date cannot be in the future.", (await JsonAsync(futureDate)).GetProperty("detail").GetString());

        Assert.Equal(HttpStatusCode.BadRequest, (await claimant.PostAsJsonAsync("/api/claims", MotorClaim(amount: 0))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await claimant.PostAsJsonAsync("/api/claims", new { })).StatusCode);
        Assert.Equal(0, _api.WithDb(db => db.Claims.Count()));
    }

    [Fact]
    public async Task Claimant_cannot_see_another_claimants_claim()
    {
        var id = await SubmitAsync(await _api.LoginAsClaimantAsync());
        var other = await OtherAsync(UserRole.Claimant);

        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/claims/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/claims/{id}/history")).StatusCode);
        Assert.Empty((await other.GetFromJsonAsync<JsonElement>("/api/claims")).EnumerateArray());
    }

    [Fact]
    public async Task Claimant_never_sees_assessment_information()
    {
        var claimant = await _api.LoginAsClaimantAsync();
        var id = await SubmitAsync(claimant);
        _api.WithDb(db =>
        {
            var officer = db.Users.Single(u => u.Email == ApiFactory.OfficerEmail);
            var claim = db.Claims.Single(c => c.Id == id);
            claim.PickUp(officer, claim.Version, TimeProvider.System);
            claim.RecordAssessment(officer, claim.Version, 98_765, TimeProvider.System);
            return db.SaveChanges();
        });

        foreach (var url in new[] { "/api/claims", $"/api/claims/{id}", $"/api/claims/{id}/history" })
        {
            var text = await claimant.GetStringAsync(url);
            Assert.DoesNotContain("assess", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("98765", text);
            Assert.DoesNotContain("officerId", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Demo Officer", text);
        }

        var officerView = await (await _api.LoginAsOfficerAsync()).GetFromJsonAsync<JsonElement>($"/api/officer/claims/{id}");
        Assert.Equal(98_765, officerView.GetProperty("assessedAmountMinor").GetInt64());
    }

    // Officer

    [Fact]
    public async Task Officer_can_view_incoming_claim_and_pick_it_up()
    {
        var id = await SubmitAsync(await _api.LoginAsClaimantAsync());
        var officer = await _api.LoginAsOfficerAsync();

        var incoming = Assert.Single((await officer.GetFromJsonAsync<JsonElement>("/api/officer/claims")).EnumerateArray());
        Assert.Equal(id, incoming.GetProperty("id").GetInt32());
        Assert.Equal("Demo Claimant", incoming.GetProperty("claimantName").GetString());

        var response = await PickUpAsync(officer, id, incoming.GetProperty("version").GetInt32());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var claim = await JsonAsync(response);
        Assert.Equal("InReview", claim.GetProperty("status").GetString());
        Assert.Equal("Demo Officer", claim.GetProperty("assignedOfficerName").GetString());
        Assert.Equal(2, claim.GetProperty("version").GetInt32());

        Assert.Single((await officer.GetFromJsonAsync<JsonElement>("/api/officer/claims")).EnumerateArray());
    }

    [Fact]
    public async Task Officer_cannot_pick_up_an_assigned_claim()
    {
        var id = await SubmitAsync(await _api.LoginAsClaimantAsync());
        var officer = await _api.LoginAsOfficerAsync();
        (await PickUpAsync(officer, id, 1)).EnsureSuccessStatusCode();
        var other = await OtherAsync(UserRole.ClaimsOfficer);

        Assert.Equal(HttpStatusCode.Conflict, (await PickUpAsync(other, id, 2)).StatusCode);

        var claim = await other.GetFromJsonAsync<JsonElement>($"/api/officer/claims/{id}");
        Assert.Equal("Demo Officer", claim.GetProperty("assignedOfficerName").GetString());
        // Another officer's claim is not in this officer's list.
        Assert.Empty((await other.GetFromJsonAsync<JsonElement>("/api/officer/claims")).EnumerateArray());
    }

    [Fact]
    public async Task Pickup_checks_version_and_claim()
    {
        var id = await SubmitAsync(await _api.LoginAsClaimantAsync());
        var officer = await _api.LoginAsOfficerAsync();

        Assert.Equal(HttpStatusCode.Conflict, (await PickUpAsync(officer, id, 99)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PickUpAsync(officer, 999, 1)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await officer.PostAsJsonAsync($"/api/officer/claims/{id}/pickup", new { })).StatusCode);
        Assert.Equal(ClaimStatus.Submitted, _api.WithDb(db => db.Claims.Single().Status));
    }

    [Fact]
    public async Task History_is_returned_in_order_to_both_roles()
    {
        var claimant = await _api.LoginAsClaimantAsync();
        var id = await SubmitAsync(claimant);
        var officer = await _api.LoginAsOfficerAsync();
        (await PickUpAsync(officer, id, 1)).EnsureSuccessStatusCode();

        foreach (var (client, url) in new[]
                 { (claimant, $"/api/claims/{id}/history"), (officer, $"/api/officer/claims/{id}/history") })
        {
            var history = (await client.GetFromJsonAsync<JsonElement>(url)).EnumerateArray().ToList();
            Assert.Equal(["Submitted", "PickedUp"], history.Select(h => h.GetProperty("action").GetString()!));
            Assert.Equal(JsonValueKind.Null, history[0].GetProperty("fromStatus").ValueKind);
            Assert.Equal("InReview", history[1].GetProperty("toStatus").GetString());
            Assert.Equal("ClaimsOfficer", history[1].GetProperty("actorRole").GetString());
        }
    }

    // Startup

    [Fact]
    public async Task OpenApi_describes_bearer_security_except_for_login()
    {
        var document = await _api.CreateClient().GetFromJsonAsync<JsonElement>("/openapi/v1.json");

        Assert.Equal("bearer", document.GetProperty("components").GetProperty("securitySchemes")
            .GetProperty("Bearer").GetProperty("scheme").GetString());
        var paths = document.GetProperty("paths");
        var security = paths.GetProperty("/api/claims").GetProperty("get").GetProperty("security");
        Assert.True(security[0].TryGetProperty("Bearer", out _));
        Assert.False(paths.GetProperty("/api/auth/login").GetProperty("post").TryGetProperty("security", out _));
        Assert.Contains("InReview", document.GetProperty("components").GetProperty("schemas").ToString());
    }

    [Fact]
    public async Task Demo_users_are_seeded_once()
    {
        Assert.Equal(2, _api.WithDb(db => db.Users.Count()));

        var hasher = new PasswordHasher<User>();
        var original = _api.WithDb(db => db.Users.Single(u => u.Email == ApiFactory.ClaimantEmail).PasswordHash);
        Assert.NotEqual(ApiFactory.ClaimantPassword, original);
        await _api.WithDb(db => DemoUserSeeder.SeedAsync(db, hasher,
            [new DemoUser(ApiFactory.ClaimantEmail, "Changed", "Changed-1", UserRole.Claimant)]));

        Assert.Equal(2, _api.WithDb(db => db.Users.Count()));
        Assert.Equal(original, _api.WithDb(db => db.Users.Single(u => u.Email == ApiFactory.ClaimantEmail).PasswordHash));
    }

    [Fact]
    public void Production_does_not_migrate_or_seed()
    {
        using var production = new ApiFactory("Production", "test-only-signing-key-with-enough-bytes");
        production.CreateClient();

        Assert.Empty(production.WithDb(db => db.Database.GetAppliedMigrations().ToList()));
    }

    [Fact]
    public void Production_needs_its_own_signing_key()
    {
        using var production = new ApiFactory("Production");

        var error = Assert.Throws<InvalidOperationException>(() => production.CreateClient());
        Assert.Contains("Jwt:Key", error.Message);
    }
}
