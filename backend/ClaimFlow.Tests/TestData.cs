using ClaimFlow.Api.Modules.Auth.Entities;
using ClaimFlow.Api.Modules.Claims;
using ClaimFlow.Api.Modules.Claims.Entities;

namespace ClaimFlow.Tests;

internal sealed class TestClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 11, 25, 4, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Now;
}

internal static class TestData
{
    public static readonly User Claimant = CreateUser(1, UserRole.Claimant);
    public static readonly User OtherClaimant = CreateUser(2, UserRole.Claimant);
    public static readonly User Officer = CreateUser(3, UserRole.ClaimsOfficer);
    public static readonly User OtherOfficer = CreateUser(4, UserRole.ClaimsOfficer);

    public static Claim Submitted(TestClock clock) =>
        Claim.Submit(Claimant, ClaimType.Motor, Market.Singapore, new DateOnly(2026, 11, 20),
            "Rear bumper damage", "SGX1234A", null, 125_050, clock);

    public static Claim InReview(TestClock clock)
    {
        var claim = Submitted(clock);
        claim.PickUp(Officer, claim.Version, clock);
        return claim;
    }

    public static Claim AwaitingInformation(TestClock clock)
    {
        var claim = InReview(clock);
        claim.RequestInformation(Officer, claim.Version, "Please describe the damage.", clock);
        return claim;
    }

    public static Claim Settled(TestClock clock)
    {
        var claim = InReview(clock);
        claim.RecordAssessment(Officer, claim.Version, 100_000, clock);
        claim.Settle(Officer, claim.Version, null, clock);
        return claim;
    }

    public static Claim Rejected(TestClock clock)
    {
        var claim = InReview(clock);
        claim.Reject(Officer, claim.Version, "Not covered.", clock);
        return claim;
    }

    public static void AssertError(ClaimRuleError expected, Action action) =>
        Assert.Equal(expected, Assert.Throws<ClaimRuleException>(action).Error);

    // Ids are normally assigned by the database.
    private static User CreateUser(int id, UserRole role)
    {
        var user = new User($"User{id}@Example.com", $"User {id}", "not-a-real-hash", role);
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, id);
        return user;
    }
}
