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

    public async Task<OfficerClaimResponse> PickUpAsync(int userId, int claimId, int expectedVersion)
    {
        var officer = await GetUserAsync(userId);
        var claim = await db.Claims.SingleOrDefaultAsync(c => c.Id == claimId) ?? throw NotFound();

        claim.PickUp(officer, expectedVersion, clock);
        await db.SaveChangesAsync();
        return await GetForOfficerAsync(claimId);
    }

    // Ownership is part of the query, so another claimant's claim is never loaded.
    private IQueryable<Claim> OwnedBy(int userId) =>
        db.Claims.AsNoTracking().Where(c => c.ClaimantId == userId);

    private IQueryable<Claim> WithUsers() =>
        db.Claims.AsNoTracking().Include(c => c.Claimant).Include(c => c.AssignedOfficer);

    private async Task<User> GetUserAsync(int userId) =>
        await db.Users.SingleOrDefaultAsync(u => u.Id == userId)
        ?? throw new UnauthorizedAccessException("The token's user no longer exists.");

    private static ClaimRuleException NotFound() => new(ClaimRuleError.NotFound, "Claim was not found.");
}
