using ClaimFlow.Api.Data;
using ClaimFlow.Api.Modules.Auth.Entities;
using ClaimFlow.Api.Modules.Claims.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClaimFlow.Api.Modules.Claims;

// Loads data, calls the lifecycle methods on Claim, and saves once. Business rules stay in Claim.
public class ClaimService(ClaimFlowDbContext db, TimeProvider clock)
{
    public async Task<ClaimantClaimResponse> SubmitAsync(int userId, SubmitClaimRequest request)
    {
        var claimant = await GetUserAsync(userId);
        var claim = Claim.Submit(claimant, request.Type!.Value, request.Market!.Value, request.IncidentDate!.Value,
            request.Description, request.VehicleRegistration, request.PropertyAddress,
            request.ClaimedAmountMinor!.Value, clock);

        db.Claims.Add(claim);
        await db.SaveChangesAsync();
        return ClaimantClaimResponse.From(claim);
    }

    public async Task<List<ClaimantClaimResponse>> ListForClaimantAsync(int userId)
    {
        var claims = await OwnedBy(userId).OrderByDescending(c => c.Id).ToListAsync();
        return claims.Select(ClaimantClaimResponse.From).ToList();
    }

    public async Task<ClaimantClaimResponse> GetForClaimantAsync(int userId, int claimId) =>
        ClaimantClaimResponse.From(
            await OwnedBy(userId).SingleOrDefaultAsync(c => c.Id == claimId) ?? throw NotFound());

    public async Task<List<ClaimantHistoryResponse>> GetHistoryForClaimantAsync(int userId, int claimId)
    {
        if (!await OwnedBy(userId).AnyAsync(c => c.Id == claimId))
        {
            throw NotFound();
        }

        var history = await db.ClaimHistory.AsNoTracking()
            .Where(h => h.ClaimId == claimId && h.Action != ClaimAction.AssessmentRecorded)
            .OrderBy(h => h.Id)
            .ToListAsync();
        return history.Select(ClaimantHistoryResponse.From).ToList();
    }

    public async Task<List<OfficerClaimResponse>> ListForOfficerAsync(int userId)
    {
        var claims = await WithUsers()
            .Where(c => c.Status == ClaimStatus.Submitted || c.AssignedOfficerId == userId)
            .OrderBy(c => c.Id)
            .ToListAsync();
        return claims.Select(OfficerClaimResponse.From).ToList();
    }

    public async Task<OfficerClaimResponse> GetForOfficerAsync(int claimId) =>
        OfficerClaimResponse.From(
            await WithUsers().SingleOrDefaultAsync(c => c.Id == claimId) ?? throw NotFound());

    public async Task<List<OfficerHistoryResponse>> GetHistoryForOfficerAsync(int claimId)
    {
        if (!await db.Claims.AnyAsync(c => c.Id == claimId))
        {
            throw NotFound();
        }

        var history = await db.ClaimHistory.AsNoTracking()
            .Include(h => h.Actor)
            .Where(h => h.ClaimId == claimId)
            .OrderBy(h => h.Id)
            .ToListAsync();
        return history.Select(OfficerHistoryResponse.From).ToList();
    }

    public Task<OfficerClaimResponse> PickUpAsync(int userId, int claimId, int expectedVersion) =>
        OfficerActionAsync(userId, claimId, (claim, officer) =>
            claim.PickUp(officer, expectedVersion, clock));

    public Task<OfficerClaimResponse> RecordAssessmentAsync(int userId, int claimId, RecordAssessmentRequest request) =>
        OfficerActionAsync(userId, claimId, (claim, officer) =>
            claim.RecordAssessment(officer, request.ExpectedVersion!.Value, request.AmountMinor ?? 0, clock));

    public Task<OfficerClaimResponse> RequestInformationAsync(int userId, int claimId, RequestInformationRequest request) =>
        OfficerActionAsync(userId, claimId, (claim, officer) =>
            claim.RequestInformation(officer, request.ExpectedVersion!.Value, request.Question ?? "", clock));

    public Task<OfficerClaimResponse> SettleAsync(int userId, int claimId, SettleClaimRequest request) =>
        OfficerActionAsync(userId, claimId, (claim, officer) =>
            claim.Settle(officer, request.ExpectedVersion!.Value, request.Note, clock));

    public Task<OfficerClaimResponse> RejectAsync(int userId, int claimId, RejectClaimRequest request) =>
        OfficerActionAsync(userId, claimId, (claim, officer) =>
            claim.Reject(officer, request.ExpectedVersion!.Value, request.Reason ?? "", clock));

    public async Task<ClaimantClaimResponse> ProvideInformationAsync(
        int userId, int claimId, ProvideInformationRequest request)
    {
        await ActAsync(userId, claimId, (claim, claimant) =>
            claim.ProvideInformation(claimant, request.ExpectedVersion!.Value, request.Response ?? "", clock));
        return await GetForClaimantAsync(userId, claimId);
    }

    private async Task<OfficerClaimResponse> OfficerActionAsync(int userId, int claimId, Action<Claim, User> action)
    {
        await ActAsync(userId, claimId, action);
        return await GetForOfficerAsync(claimId);
    }

    // The claim, its requests and its history are saved together by one SaveChanges call.
    private async Task ActAsync(int userId, int claimId, Action<Claim, User> action)
    {
        var user = await GetUserAsync(userId);
        var claim = await db.Claims.Include(c => c.InformationRequests).SingleOrDefaultAsync(c => c.Id == claimId)
            ?? throw NotFound();

        action(claim, user);
        await db.SaveChangesAsync();
    }

    // Ownership is part of the query, so another claimant's claim is never read.
    private IQueryable<Claim> OwnedBy(int userId) =>
        db.Claims.AsNoTracking().Include(c => c.InformationRequests).Where(c => c.ClaimantId == userId);

    private IQueryable<Claim> WithUsers() =>
        db.Claims.AsNoTracking()
            .Include(c => c.Claimant)
            .Include(c => c.AssignedOfficer)
            .Include(c => c.InformationRequests);

    private async Task<User> GetUserAsync(int userId) =>
        await db.Users.SingleOrDefaultAsync(u => u.Id == userId)
        ?? throw new UnauthorizedAccessException("The token's user no longer exists.");

    private static ClaimRuleException NotFound() => new(ClaimRuleError.NotFound, "Claim was not found.");
}
