using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ClaimFlow.Api.Modules.Auth;

public record JwtSettings(string Issuer, string Audience, SymmetricSecurityKey SigningKey)
{
    public static JwtSettings Load(IConfiguration configuration)
    {
        var key = configuration["Jwt:Key"];
        if (key is null || Encoding.UTF8.GetByteCount(key) < 32)
        {
            throw new InvalidOperationException("Jwt:Key must be configured with at least 32 bytes.");
        }

        return new JwtSettings(
            configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured."),
            configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured."),
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)));
    }
}
