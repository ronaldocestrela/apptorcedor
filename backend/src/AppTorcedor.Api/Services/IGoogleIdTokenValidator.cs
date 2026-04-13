namespace AppTorcedor.Api.Services;

public sealed record GoogleValidatedUser(string Subject, string Email, string? Name, bool EmailVerified);

public interface IGoogleIdTokenValidator
{
    Task<GoogleValidatedUser?> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}
