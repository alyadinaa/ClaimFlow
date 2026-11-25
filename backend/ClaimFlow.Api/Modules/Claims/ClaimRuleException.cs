namespace ClaimFlow.Api.Modules.Claims;

// Each value maps to one HTTP status when the API is added.
public enum ClaimRuleError
{
    Validation, // 400
    Forbidden,  // 403
    NotFound,   // 404
    Conflict    // 409
}

public class ClaimRuleException(ClaimRuleError error, string message) : Exception(message)
{
    public ClaimRuleError Error { get; } = error;
}
