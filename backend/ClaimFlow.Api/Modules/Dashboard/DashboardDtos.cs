using ClaimFlow.Api.Modules.Claims.Entities;

namespace ClaimFlow.Api.Modules.Dashboard;

public record DashboardResponse(
    int TotalClaims,
    int UnassignedClaims,
    int OpenClaims,
    int DecidedClaims,
    List<StatusCount> ClaimsByStatus,
    List<OfficerWorkload> Officers);

public record StatusCount(ClaimStatus Status, int Count);

// AverageSecondsToDecision is null when the officer has decided no claims.
public record OfficerWorkload(
    int OfficerId,
    string OfficerName,
    int OpenAssigned,
    int AwaitingInformation,
    int Decided,
    double? AverageSecondsToDecision);

// One row per currency. There is deliberately no total across currencies.
public record CurrencyExposure(string CurrencyCode, int CurrencyDecimalPlaces, int OpenClaims, long ExposureMinor);
