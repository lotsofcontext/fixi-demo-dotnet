using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace GMVM.EnergyTracker.Tests.Integration;

/// <summary>
/// Genera JWTs validos para el TestWebApplicationFactory.
/// Usa la misma key/issuer/audience que <c>Program.cs</c>.
/// </summary>
public static class JwtTokenHelper
{
    private const string Issuer = "GMVM.EnergyTracker";
    private const string Audience = "GMVM.EnergyTracker.Api";
    private const string Key = "DEMO_ONLY_REPLACE_ME_IN_PRODUCTION_AT_LEAST_32_CHARS";

    public static string GenerateToken(string email, string role)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, email),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
