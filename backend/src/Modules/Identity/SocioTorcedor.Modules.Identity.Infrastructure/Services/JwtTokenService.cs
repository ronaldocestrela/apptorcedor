using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SocioTorcedor.Modules.Identity.Application.Contracts;
using SocioTorcedor.Modules.Identity.Infrastructure.Options;

namespace SocioTorcedor.Modules.Identity.Infrastructure.Services;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _jwt = options.Value;

    public (string Token, DateTime ExpiresAtUtc) CreateAccessToken(
        string userId,
        string email,
        Guid tenantId,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions)
    {
        if (string.IsNullOrWhiteSpace(_jwt.SigningKey))
            throw new InvalidOperationException("Jwt:SigningKey is not configured.");

        var expires = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("tenant_id", tenantId.ToString())
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        foreach (var permission in permissions)
            claims.Add(new Claim("permission", permission));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: creds);

        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return (encoded, expires);
    }
}
