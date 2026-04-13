using AppTorcedor.Api.Services;

namespace AppTorcedor.Api.Tests;

/// <summary>Test double: accepts only <c>test-google-token</c> as a valid Google ID token.</summary>
public sealed class FakeGoogleIdTokenValidator : IGoogleIdTokenValidator
{
    public const string ValidTestToken = "test-google-token";

    public Task<GoogleValidatedUser?> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (idToken != ValidTestToken)
            return Task.FromResult<GoogleValidatedUser?>(null);

        return Task.FromResult<GoogleValidatedUser?>(
            new GoogleValidatedUser("google-test-subject", "google-user@test.local", "Google Test User", true));
    }
}
