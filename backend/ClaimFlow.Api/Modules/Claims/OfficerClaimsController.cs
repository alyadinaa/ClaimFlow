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

    [HttpPost("{id:int}/assessment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OfficerClaimResponse>> RecordAssessment(int id, RecordAssessmentRequest request) =>
        await claims.RecordAssessmentAsync(User.GetUserId(), id, request);

    [HttpPost("{id:int}/information-requests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OfficerClaimResponse>> RequestInformation(int id, RequestInformationRequest request) =>
        await claims.RequestInformationAsync(User.GetUserId(), id, request);

    [HttpPost("{id:int}/settle")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OfficerClaimResponse>> Settle(int id, SettleClaimRequest request) =>
        await claims.SettleAsync(User.GetUserId(), id, request);

    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OfficerClaimResponse>> Reject(int id, RejectClaimRequest request) =>
        await claims.RejectAsync(User.GetUserId(), id, request);
}
