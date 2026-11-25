using ClaimFlow.Api.Data;
using ClaimFlow.Api.Modules.Auth.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ClaimFlow.Api.Modules.Auth;

public class AuthService(
    ClaimFlowDbContext db, IPasswordHasher<User> hasher, JwtSettings jwt, TimeProvider clock)
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    // Returns null for an unknown email or a wrong password, so the caller cannot tell them apart.
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var email = request.Email.ToLowerInvariant();
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Email == email);
        if (user is null)
        {
            // Do similar work to a real check, so response time does not reveal which emails exist.
            hasher.HashPassword(null!, request.Password);
            return null;
        }

        if (hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return null;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var expires = now.Add(TokenLifetime);
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            Claims = new Dictionary<string, object>
            {
                [CurrentUser.IdClaim] = user.Id.ToString(),
                [CurrentUser.RoleClaim] = user.Role.ToString()
            },
            SigningCredentials = new SigningCredentials(jwt.SigningKey, SecurityAlgorithms.HmacSha256)
        });

        return new LoginResponse(token, expires, user.DisplayName, user.Role);
    }
}
