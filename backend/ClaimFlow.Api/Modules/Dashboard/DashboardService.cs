using ClaimFlow.Api.Data;
using ClaimFlow.Api.Modules.Auth.Entities;
using ClaimFlow.Api.Modules.Claims;
using ClaimFlow.Api.Modules.Claims.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClaimFlow.Api.Modules.Dashboard;

// Read-only team figures.
public class DashboardService(ClaimFlowDbContext db)
{
    public async Task<DashboardResponse> GetAsync()
    {
        // Counted by the database, one row per officer and status.
        var counts = await db.Claims
            .GroupBy(c => new { c.AssignedOfficerId, c.Status })
            .Select(g => new { g.Key.AssignedOfficerId, g.Key.Status, Count = g.Count() })
            .ToListAsync();

        // EF Core cannot subtract dates in SQLite, so only the needed columns are read and averaged here.
        var decisions = await db.Claims
            .Where(c => c.DecidedAtUtc != null)
            .Select(c => new { c.AssignedOfficerId, c.SubmittedAtUtc, DecidedAtUtc = c.DecidedAtUtc!.Value })
            .ToListAsync();
        var averageSeconds = decisions
            .GroupBy(d => d.AssignedOfficerId!.Value)
            .ToDictionary(g => g.Key, g => g.Average(d => (d.DecidedAtUtc - d.SubmittedAtUtc).TotalSeconds));

        var officers = await db.Users
            .Where(u => u.Role == UserRole.ClaimsOfficer)
            .OrderBy(u => u.DisplayName)
            .ThenBy(u => u.Id)
            .Select(u => new { u.Id, u.DisplayName })
            .ToListAsync();

        int CountFor(int officerId, params ClaimStatus[] statuses) => counts
            .Where(c => c.AssignedOfficerId == officerId && statuses.Contains(c.Status))
            .Sum(c => c.Count);

        var byStatus = Enum.GetValues<ClaimStatus>()
            .Select(status => new StatusCount(status, counts.Where(c => c.Status == status).Sum(c => c.Count)))
            .ToList();
        var total = byStatus.Sum(s => s.Count);
        var decided = byStatus
            .Where(s => s.Status is ClaimStatus.Settled or ClaimStatus.Rejected)
            .Sum(s => s.Count);

        return new DashboardResponse(
            total,
            counts.Where(c => c.AssignedOfficerId == null).Sum(c => c.Count),
            total - decided,
            decided,
            byStatus,
            officers.Select(o => new OfficerWorkload(
                o.Id,
                o.DisplayName,
                CountFor(o.Id, ClaimStatus.InReview, ClaimStatus.AwaitingInformation),
                CountFor(o.Id, ClaimStatus.AwaitingInformation),
                CountFor(o.Id, ClaimStatus.Settled, ClaimStatus.Rejected),
                averageSeconds.TryGetValue(o.Id, out var average) ? average : null)).ToList());
    }

    // Open claims only: the assessed amount if there is one, otherwise the claimed amount.
    public async Task<List<CurrencyExposure>> GetExposureAsync()
    {
        var totals = await db.Claims
            .Where(c => c.Status != ClaimStatus.Settled && c.Status != ClaimStatus.Rejected)
            .GroupBy(c => c.CurrencyCode)
            .Select(g => new
            {
                CurrencyCode = g.Key,
                OpenClaims = g.Count(),
                ExposureMinor = g.Sum(c => c.AssessedAmountMinor ?? c.ClaimedAmountMinor)
            })
            .OrderBy(t => t.CurrencyCode)
            .ToListAsync();

        return totals
            .Select(t => new CurrencyExposure(
                t.CurrencyCode, MarketCatalog.DecimalPlacesFor(t.CurrencyCode), t.OpenClaims, t.ExposureMinor))
            .ToList();
    }
}
