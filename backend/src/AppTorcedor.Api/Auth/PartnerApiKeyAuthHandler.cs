using System.Security.Claims;
using System.Text.Encodings.Web;
using AppTorcedor.Application.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AppTorcedor.Api.Auth;

/// <summary>
/// Handler de autenticação que valida o header <c>X-Api-Key</c> contra as chaves de parceiros.
/// A chave nunca é logada em texto claro.
/// </summary>
public sealed class PartnerApiKeyAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IPartnerApiKeyPort partnerApiKeyPort)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    public const string SchemeName = "PartnerApiKey";
    public const string HeaderName = "X-Api-Key";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var headerValues))
            return AuthenticateResult.NoResult();

        var rawKey = headerValues.ToString();
        if (string.IsNullOrWhiteSpace(rawKey))
            return AuthenticateResult.Fail("API key is empty.");

        ValidatedPartnerKeyDto? partner;
        try
        {
            partner = await partnerApiKeyPort.ValidateAsync(rawKey, Context.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating partner API key.");
            return AuthenticateResult.Fail("Internal error during API key validation.");
        }

        if (partner is null)
        {
            Logger.LogWarning("Invalid or revoked API key received for partner endpoint.");
            return AuthenticateResult.Fail("Invalid or revoked API key.");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, partner.Id.ToString()),
            new Claim(ClaimTypes.Name, partner.Name),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }
}
