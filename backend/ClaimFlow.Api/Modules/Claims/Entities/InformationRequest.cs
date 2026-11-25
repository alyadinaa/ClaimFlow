using ClaimFlow.Api.Modules.Auth.Entities;

namespace ClaimFlow.Api.Modules.Claims.Entities;

public class InformationRequest
{
    private InformationRequest()
    {
    }

    internal InformationRequest(Claim claim, User requestedBy, string question, DateTime requestedAtUtc)
    {
        Claim = claim;
        RequestedById = requestedBy.Id;
        Question = question;
        RequestedAtUtc = requestedAtUtc;
        Status = InformationRequestStatus.Open;
    }

    internal void Answer(string response, DateTime respondedAtUtc)
    {
        Response = response;
        RespondedAtUtc = respondedAtUtc;
        Status = InformationRequestStatus.Answered;
    }

    internal void Close()
    {
        Status = InformationRequestStatus.Closed;
    }

    public int Id { get; private set; }

    public int ClaimId { get; private set; }

    public Claim Claim { get; private set; } = null!;

    public int RequestedById { get; private set; }

    public User RequestedBy { get; private set; } = null!;

    public string Question { get; private set; } = null!;

    public DateTime RequestedAtUtc { get; private set; }

    public InformationRequestStatus Status { get; private set; }

    public string? Response { get; private set; }

    public DateTime? RespondedAtUtc { get; private set; }
}
