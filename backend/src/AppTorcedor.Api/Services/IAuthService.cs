using System.Security.Claims;
using AppTorcedor.Api.Contracts;

namespace AppTorcedor.Api.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<AuthResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<MeResponse?> GetMeAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

    Task<AuthResponse?> AcceptStaffInviteAsync(
        string token,
        string password,
        string? name,
        CancellationToken cancellationToken = default);

    Task<AuthResponse?> IssueSessionForUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<AuthResponse?> SignInWithGoogleAsync(GoogleSignInRequest request, CancellationToken cancellationToken = default);

    Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);

    Task<PasswordResetResult> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default);
}
