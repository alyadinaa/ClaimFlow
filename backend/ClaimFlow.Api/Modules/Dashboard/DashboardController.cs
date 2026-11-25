using ClaimFlow.Api.Modules.Auth.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaimFlow.Api.Modules.Dashboard;

[ApiController]
[Route("api/officer/dashboard")]
[Authorize(Roles = nameof(UserRole.ClaimsOfficer))]
public class DashboardController(DashboardService dashboard) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> Get() => await dashboard.GetAsync();

    [HttpGet("exposure")]
    public async Task<ActionResult<List<CurrencyExposure>>> Exposure() => await dashboard.GetExposureAsync();
}
