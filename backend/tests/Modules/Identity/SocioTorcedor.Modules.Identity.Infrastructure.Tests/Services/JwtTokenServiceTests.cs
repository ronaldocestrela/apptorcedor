using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using SocioTorcedor.Modules.Identity.Infrastructure.Options;
using SocioTorcedor.Modules.Identity.Infrastructure.Services;

namespace SocioTorcedor.Modules.Identity.Infrastructure.Tests.Services;

public class JwtTokenServiceTests
{
    [Fact]
    public void CreateAccessToken_contains_roles_permissions_tenant_and_validates()
    {
        var keyString = new string('x', 64);
        var options = Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = "issuer",
            Audience = "audience",
            SigningKey = keyString,
            AccessTokenMinutes = 60
        });

        var svc = new JwtTokenService(options);
        var tenantId = Guid.NewGuid();
        var (token, expires) = svc.CreateAccessToken(
            "user-id",
            "u@test.com",
            tenantId,
            new[] { "Socio" },
            new[] { "Socios.Visualizar" });

        expires.Should().BeAfter(DateTime.UtcNow);

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "issuer",
            ValidAudience = "audience",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString)),
            ClockSkew = TimeSpan.Zero
        };

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, parameters, out _);

        principal.FindFirstValue(ClaimTypes.Role).Should().Be("Socio");
        principal.FindAll("permission").Select(c => c.Value).Should().Contain("Socios.Visualizar");
        principal.FindFirstValue("tenant_id").Should().Be(tenantId.ToString());
        principal.FindFirstValue(ClaimTypes.Email).Should().Be("u@test.com");
    }
}
