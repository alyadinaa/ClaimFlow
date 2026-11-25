using ClaimFlow.Api.Modules.Auth;
using ClaimFlow.Api.Modules.Auth.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaimFlow.Api.Modules.Claims;

[ApiController]
[Route("api/claims")]
[Authorize(Roles = nameof(UserRole.Claimant))]
public class ClaimsController(ClaimService claims) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClaimantClaimResponse>> Submit(SubmitClaimRequest request)
    {
        var claim = await claims.SubmitAsync(User.GetUserId(), request);
        return CreatedAtAction(nameof(Get), new { id = claim.Id }, claim);
    }

    [HttpGet]
    public async Task<ActionResult<List<ClaimantClaimResponse>>> List() =>
        await claims.ListForClaimantAsync(User.GetUserId());

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaimantClaimResponse>> Get(int id) =>
        await claims.GetForClaimantAsync(User.GetUserId(), id);

    [HttpGet("{id:int}/history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ClaimantHistoryResponse>>> History(int id) =>
        await claims.GetHistoryForClaimantAsync(User.GetUserId(), id);
}
