using System.Security.Claims;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Api.Options;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AppTorcedor.Api.Services;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    IRefreshTokenStore refreshTokens,
    IJwtTokenIssuer jwt,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    public async Task<AuthResponse?> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (user is null || !user.IsActive)
            return null;
        if (!await userManager.CheckPasswordAsync(user, password).ConfigureAwait(false))
            return null;

        var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        var (access, expiresIn) = jwt.IssueAccessToken(user, roles);
        var refreshLifetime = TimeSpan.FromDays(jwtOptions.Value.RefreshTokenDays);
        var refresh = await refreshTokens.CreateAsync(user.Id, refreshLifetime, cancellationToken).ConfigureAwait(false);
        return new AuthResponse(access, refresh, expiresIn, roles.ToList());
    }

    public async Task<AuthResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var newLifetime = TimeSpan.FromDays(jwtOptions.Value.RefreshTokenDays);
        var rotated = await refreshTokens.RotateAsync(refreshToken, newLifetime, cancellationToken).ConfigureAwait(false);
        if (rotated is null)
            return null;

        var user = await userManager.FindByIdAsync(rotated.Value.UserId.ToString()).ConfigureAwait(false);
        if (user is null || !user.IsActive)
            return null;

        var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        var (access, expiresIn) = jwt.IssueAccessToken(user, roles);
        return new AuthResponse(access, rotated.Value.NewPlainRefreshToken, expiresIn, roles.ToList());
    }

    public Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        refreshTokens.RevokeAsync(refreshToken, cancellationToken);

    public async Task<MeResponse?> GetMeAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id))
            return null;
        var user = await userManager.FindByIdAsync(id).ConfigureAwait(false);
        if (user is null || !user.IsActive)
            return null;
        var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        return new MeResponse(user.Id, user.Email ?? string.Empty, user.Name, roles.ToList());
    }
}
