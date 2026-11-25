using ClaimFlow.Api.Data;
using ClaimFlow.Api.Modules.Auth.Entities;
using ClaimFlow.Api.Modules.Claims.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ClaimFlow.Tests;

// Each test gets its own in-memory database, which exists while the connection stays open.
public sealed class PersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:;Foreign Keys=True");
    private readonly TestClock _clock = new();
    private readonly User _claimant = new("Claimant@Example.com", "Demo Claimant", "not-a-real-hash", UserRole.Claimant);
    private readonly User _officer = new("officer@example.com", "Demo Officer", "not-a-real-hash", UserRole.ClaimsOfficer);

    public PersistenceTests()
    {
        _connection.Open();

        using var db = NewContext();
        db.Database.Migrate();
        db.Users.Add(_claimant);
        db.SaveChanges();
        db.Users.Add(_officer);
        db.SaveChanges();
    }

    public void Dispose() => _connection.Dispose();

    // A new context has no cached entities, so it must read from the database.
    private ClaimFlowDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ClaimFlowDbContext>().UseSqlite(_connection).Options);

    private static Claim Load(ClaimFlowDbContext db, int id) =>
        db.Claims.Include(c => c.History).Include(c => c.InformationRequests).Single(c => c.Id == id);

    private int SaveSubmitted()
    {
        using var db = NewContext();
        var claim = Claim.Submit(_claimant, ClaimType.Motor, Market.Singapore, new DateOnly(2026, 11, 20),
            "Rear bumper damage", "SGX1234A", null, 125_050, _clock);
        db.Claims.Add(claim);
        db.SaveChanges();
        return claim.Id;
    }

    private void Change(int id, Action<Claim> action)
    {
        using var db = NewContext();
        action(Load(db, id));
        db.SaveChanges();
    }

    private int SaveInReview()
    {
        var id = SaveSubmitted();
        Change(id, c => c.PickUp(_officer, c.Version, _clock));
        return id;
    }

    private int SaveAwaitingInformation()
    {
        var id = SaveInReview();
        Change(id, c => c.RequestInformation(_officer, c.Version, "Any photos?", _clock));
        return id;
    }

    private object? Scalar(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar();
    }

    private void AssertDatabaseRejects(string sql, string expectedMessage)
    {
        var error = Assert.Throws<SqliteException>(() => Scalar(sql));
        Assert.Contains(expectedMessage, error.Message);
    }

    [Fact]
    public void Migration_applies_and_matches_the_model()
    {
        using var db = NewContext();

        Assert.Single(db.Database.GetAppliedMigrations());
        Assert.Empty(db.Database.GetPendingMigrations());
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Equal(4L, Scalar(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name IN ('Users','Claims','ClaimHistory','InformationRequests')"));
    }

    [Fact]
    public void Claim_and_history_round_trip()
    {
        var id = SaveSubmitted();

        using var db = NewContext();
        var claim = Load(db, id);

        Assert.Equal(_claimant.Id, claim.ClaimantId);
        Assert.Equal(ClaimType.Motor, claim.Type);
        Assert.Equal(Market.Singapore, claim.Market);
        Assert.Equal("SGD", claim.CurrencyCode);
        Assert.Equal(new DateOnly(2026, 11, 20), claim.IncidentDate);
        Assert.Equal(125_050, claim.ClaimedAmountMinor);
        Assert.Null(claim.AssignedOfficerId);
        Assert.Equal(1, claim.Version);
        Assert.Equal(_clock.Now.UtcDateTime, claim.SubmittedAtUtc);
        Assert.Equal(DateTimeKind.Utc, claim.SubmittedAtUtc.Kind);

        var history = Assert.Single(claim.History);
        Assert.Equal(ClaimAction.Submitted, history.Action);
        Assert.Null(history.FromStatus);
        Assert.Equal(_claimant.Id, history.ActorId);
        Assert.Equal(UserRole.Claimant, history.ActorRole);
    }

    [Fact]
    public void Values_are_stored_in_the_approved_form()
    {
        SaveSubmitted();

        Assert.Equal("Submitted", Scalar("SELECT Status FROM Claims"));
        Assert.Equal("2026-11-20", Scalar("SELECT IncidentDate FROM Claims"));
        Assert.Equal("integer", Scalar("SELECT typeof(ClaimedAmountMinor) FROM Claims"));
        Assert.Equal("claimant@example.com", Scalar($"SELECT Email FROM Users WHERE Id = {_claimant.Id}"));
    }

    [Fact]
    public void Settlement_persists_amounts_decision_time_and_history()
    {
        var id = SaveInReview();
        Change(id, c => c.RecordAssessment(_officer, c.Version, 80_000, _clock));
        Change(id, c => c.Settle(_officer, c.Version, "Approved.", _clock));

        using var db = NewContext();
        var claim = Load(db, id);
        Assert.Equal(ClaimStatus.Settled, claim.Status);
        Assert.Equal(80_000, claim.SettlementAmountMinor);
        Assert.Equal(DateTimeKind.Utc, claim.DecidedAtUtc!.Value.Kind);
        Assert.Equal(4, claim.Version);

        var history = claim.History.OrderBy(h => h.Id).ToList();
        Assert.Equal(
            [ClaimAction.Submitted, ClaimAction.PickedUp, ClaimAction.AssessmentRecorded, ClaimAction.Settled],
            history.Select(h => h.Action));
        Assert.Equal(80_000, history[^1].AmountMinor);
        Assert.Equal("SGD", history[^1].CurrencyCode);
    }

    [Fact]
    public void Request_and_response_history_share_one_request()
    {
        var id = SaveAwaitingInformation();
        Change(id, c => c.ProvideInformation(_claimant, c.Version, "No photos.", _clock));

        using var db = NewContext();
        var claim = Load(db, id);
        var request = Assert.Single(claim.InformationRequests);
        Assert.Equal(InformationRequestStatus.Answered, request.Status);
        Assert.Equal("No photos.", request.Response);
        Assert.Equal(_officer.Id, request.RequestedById);

        Assert.Equal(
            [ClaimAction.InformationRequested, ClaimAction.InformationProvided],
            claim.History.Where(h => h.InformationRequestId == request.Id).OrderBy(h => h.Id).Select(h => h.Action));
    }

    [Fact]
    public void T8_persists_rejection_closure_and_history_together()
    {
        var id = SaveAwaitingInformation();
        Change(id, c => c.Reject(_officer, c.Version, "No reply.", _clock));

        using var db = NewContext();
        var claim = Load(db, id);
        Assert.Equal(ClaimStatus.Rejected, claim.Status);
        Assert.Equal("No reply.", claim.RejectionReason);
        Assert.NotNull(claim.DecidedAtUtc);

        var request = Assert.Single(claim.InformationRequests);
        Assert.Equal(InformationRequestStatus.Closed, request.Status);

        var history = claim.History.OrderBy(h => h.Id).Last();
        Assert.Equal(ClaimAction.Rejected, history.Action);
        Assert.Equal(ClaimStatus.AwaitingInformation, history.FromStatus);
        Assert.Equal(request.Id, history.InformationRequestId);
    }

    // T8 writes to three tables. Failing each write in turn proves that the others are
    // rolled back, whatever order EF uses.
    [Theory]
    [InlineData("BEFORE UPDATE ON Claims")]
    [InlineData("BEFORE UPDATE ON InformationRequests")]
    [InlineData("BEFORE INSERT ON ClaimHistory")]
    public void T8_saves_nothing_if_any_write_fails(string failingWrite)
    {
        var id = SaveAwaitingInformation();
        Scalar($"CREATE TRIGGER FailWrite {failingWrite} BEGIN SELECT RAISE(ABORT, 'forced failure'); END");

        Assert.Throws<DbUpdateException>(() => Change(id, c => c.Reject(_officer, c.Version, "No reply.", _clock)));

        using var db = NewContext();
        var claim = Load(db, id);
        Assert.Equal(ClaimStatus.AwaitingInformation, claim.Status);
        Assert.Null(claim.RejectionReason);
        Assert.Equal(3, claim.Version);
        Assert.Equal(3, claim.History.Count);
        Assert.Equal(InformationRequestStatus.Open, Assert.Single(claim.InformationRequests).Status);
    }

    [Fact]
    public void Stale_update_is_detected_and_saves_nothing()
    {
        var id = SaveSubmitted();
        using var first = NewContext();
        using var second = NewContext();
        var firstCopy = Load(first, id);
        var secondCopy = Load(second, id);

        // Both copies pass the domain checks, because both were loaded at version 1.
        firstCopy.PickUp(_officer, 1, _clock);
        secondCopy.PickUp(_officer, 1, _clock);
        first.SaveChanges();

        Assert.Throws<DbUpdateConcurrencyException>(() => second.SaveChanges());

        using var db = NewContext();
        var claim = Load(db, id);
        Assert.Equal(2, claim.Version);
        Assert.Equal(2, claim.History.Count);
    }

    // The API maps this exception to 409; the unique index could otherwise fail first.
    [Fact]
    public void Two_stale_information_requests_fail_as_a_concurrency_conflict()
    {
        var id = SaveInReview();
        using var first = NewContext();
        using var second = NewContext();
        Load(first, id).RequestInformation(_officer, 2, "First question", _clock);
        Load(second, id).RequestInformation(_officer, 2, "Second question", _clock);
        first.SaveChanges();

        Assert.Throws<DbUpdateConcurrencyException>(() => second.SaveChanges());

        using var db = NewContext();
        Assert.Equal("First question", Assert.Single(Load(db, id).InformationRequests).Question);
    }

    [Theory]
    [InlineData("AssignedOfficerId = 2", "CK_Claims_Assignment")]
    [InlineData("ClaimedAmountMinor = 0", "CK_Claims_ClaimedAmount")]
    [InlineData("AssessedAmountMinor = 0", "CK_Claims_AssessedAmount")]
    [InlineData("Status = 'Settled', AssignedOfficerId = 2, DecidedAtUtc = '2026-11-25', SettlementAmountMinor = 0", "CK_Claims_SettlementAmount")]
    [InlineData("SettlementAmountMinor = 5", "CK_Claims_Settlement")]
    [InlineData("RejectionReason = 'x'", "CK_Claims_Rejection")]
    [InlineData("DecidedAtUtc = '2026-11-25'", "CK_Claims_Decided")]
    [InlineData("VehicleRegistration = NULL", "CK_Claims_Vehicle")]
    [InlineData("PropertyAddress = 'x'", "CK_Claims_Property")]
    public void Claim_check_constraints_reject_invalid_rows(string change, string constraint)
    {
        Assert.Equal(2, _officer.Id);
        SaveSubmitted();

        // The closing text stops one constraint name matching a longer one.
        AssertDatabaseRejects($"UPDATE Claims SET {change}", $"CHECK constraint failed: {constraint}'");
    }

    [Theory]
    [InlineData("Response = 'x'")]
    [InlineData("RespondedAtUtc = '2026-11-25'")]
    [InlineData("Status = 'Answered'")]
    public void Answer_check_constraint_rejects_invalid_rows(string change)
    {
        SaveAwaitingInformation();
        AssertDatabaseRejects($"UPDATE InformationRequests SET {change}", "CK_InformationRequests_Answered");
    }

    [Fact]
    public void Only_one_open_request_is_allowed_per_claim()
    {
        var id = SaveAwaitingInformation();

        AssertDatabaseRejects(InsertRequest(id, "Open"), "UNIQUE constraint failed: InformationRequests.ClaimId");

        // The index is filtered, so other rows for the same claim and open rows for other claims are allowed.
        Scalar(InsertRequest(id, "Closed"));
        SaveAwaitingInformation();
        Assert.Equal(3L, Scalar("SELECT COUNT(*) FROM InformationRequests"));
    }

    [Fact]
    public void Email_must_be_unique()
    {
        using var db = NewContext();
        db.Users.Add(new User("CLAIMANT@example.com", "Duplicate", "not-a-real-hash", UserRole.Claimant));

        var error = Assert.Throws<DbUpdateException>(() => db.SaveChanges());
        Assert.Contains("UNIQUE constraint failed: Users.Email", error.InnerException!.Message);
    }

    [Theory]
    [InlineData("DELETE FROM Users WHERE Id = 1")]
    [InlineData("DELETE FROM Users WHERE Id = 2")]
    [InlineData("DELETE FROM Claims")]
    [InlineData("DELETE FROM InformationRequests")]
    [InlineData("UPDATE Claims SET ClaimantId = 999")]
    public void Foreign_keys_are_enforced(string sql)
    {
        SaveAwaitingInformation();
        AssertDatabaseRejects(sql, "FOREIGN KEY constraint failed");
    }

    private string InsertRequest(int claimId, string status) =>
        "INSERT INTO InformationRequests (ClaimId, RequestedById, Question, RequestedAtUtc, Status) " +
        $"VALUES ({claimId}, {_officer.Id}, 'Another question', '2026-11-25', '{status}')";
}
