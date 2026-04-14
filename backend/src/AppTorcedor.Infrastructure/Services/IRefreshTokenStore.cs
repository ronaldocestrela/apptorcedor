using AppTorcedor.Identity;

namespace AppTorcedor.Infrastructure.Services;

public interface IRefreshTokenStore
{
    /// <summary>Creates a refresh token for the user; returns the plain token once.</summary>
    Task<string> CreateAsync(Guid userId, TimeSpan lifetime, CancellationToken cancellationToken = default);

    /// <summary>Validates plain token, revokes it, creates a new refresh token; returns user id and new plain refresh.</summary>
    Task<(Guid UserId, string NewPlainRefreshToken)?> RotateAsync(
        string plainRefreshToken,
        TimeSpan newLifetime,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(string plainRefreshToken, CancellationToken cancellationToken = default);

    Task<ApplicationUser?> FindUserByRefreshTokenAsync(
        string plainRefreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>Revokes all refresh tokens for the user (e.g. after anonymization).</summary>
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
