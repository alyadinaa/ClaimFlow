using ClaimFlow.Api.Modules.Auth;
using ClaimFlow.Api.Modules.Auth.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaimFlow.Api.Modules.Claims;

[ApiController]
[Route("api/officer/claims")]
[Authorize(Roles = nameof(UserRole.ClaimsOfficer))]
public class OfficerClaimsController(ClaimService claims) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<OfficerClaimResponse>>> List() =>
        await claims.ListForOfficerAsync(User.GetUserId());

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OfficerClaimResponse>> Get(int id) =>
        await claims.GetForOfficerAsync(id);

    [HttpGet("{id:int}/history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<OfficerHistoryResponse>>> History(int id) =>
        await claims.GetHistoryForOfficerAsync(id);

    [HttpPost("{id:int}/pickup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OfficerClaimResponse>> PickUp(int id, PickUpClaimRequest request) =>
        await claims.PickUpAsync(User.GetUserId(), id, request.ExpectedVersion!.Value);
}
