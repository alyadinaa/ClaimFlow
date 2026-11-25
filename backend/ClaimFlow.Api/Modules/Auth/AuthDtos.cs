using System.ComponentModel.DataAnnotations;
using ClaimFlow.Api.Modules.Auth.Entities;

namespace ClaimFlow.Api.Modules.Auth;

public record LoginRequest(
    [Required, MaxLength(254)] string Email,
    [Required, MaxLength(200)] string Password);

public record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, string DisplayName, UserRole Role);
