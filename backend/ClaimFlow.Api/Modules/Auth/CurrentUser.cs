using System.Security.Claims;

namespace ClaimFlow.Api.Modules.Auth;

public static class CurrentUser
{
    public const string IdClaim = "sub";
    public const string RoleClaim = "role";

    // The user's identity always comes from the validated token, never from the request.
    public static int GetUserId(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(IdClaim), out var id)
            ? id
            : throw new UnauthorizedAccessException("The token does not identify a user.");
}
