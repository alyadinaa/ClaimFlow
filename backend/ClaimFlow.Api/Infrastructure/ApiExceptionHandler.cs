using ClaimFlow.Api.Modules.Claims;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClaimFlow.Api.Infrastructure;

// Turns expected errors into HTTP responses. Anything else stays a 500 without details.
public class ApiExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        (int Status, string Detail)? error = exception switch
        {
            ClaimRuleException rule => (StatusFor(rule.Error), rule.Message),
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict, "The claim has changed. Reload it and try again."),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Please log in again."),
            _ => null
        };

        if (error is null)
        {
            return false;
        }

        context.Response.StatusCode = error.Value.Status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails { Status = error.Value.Status, Detail = error.Value.Detail }
        });
    }

    private static int StatusFor(ClaimRuleError error) => error switch
    {
        ClaimRuleError.Validation => StatusCodes.Status400BadRequest,
        ClaimRuleError.Forbidden => StatusCodes.Status403Forbidden,
        ClaimRuleError.NotFound => StatusCodes.Status404NotFound,
        ClaimRuleError.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };
}
