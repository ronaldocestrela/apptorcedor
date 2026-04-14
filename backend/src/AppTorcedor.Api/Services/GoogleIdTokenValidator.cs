using AppTorcedor.Api.Options;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace AppTorcedor.Api.Services;

public sealed class GoogleIdTokenValidator(IOptions<GoogleAuthOptions> options) : IGoogleIdTokenValidator
{
    public async Task<GoogleValidatedUser?> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
            return null;

        var clientId = options.Value.ClientId?.Trim();
        if (string.IsNullOrEmpty(clientId))
            return null;

        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings { Audience = [clientId] };
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings).ConfigureAwait(false);
            if (payload is null || string.IsNullOrWhiteSpace(payload.Subject) || string.IsNullOrWhiteSpace(payload.Email))
                return null;
            if (payload.EmailVerified != true)
                return null;

            return new GoogleValidatedUser(payload.Subject, payload.Email.Trim(), payload.Name, payload.EmailVerified == true);
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}
