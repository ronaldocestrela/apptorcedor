using System.Security.Claims;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Api.Options;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AppTorcedor.Api.Services;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    IRefreshTokenStore refreshTokens,
    IPermissionResolver permissionResolver,
    IStaffAdministrationPort staffAdministration,
    ITorcedorAccountPort torcedorAccount,
    IGoogleIdTokenValidator googleTokens,
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

        return await IssueSessionAsync(user, cancellationToken).ConfigureAwait(false);
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

        return await IssueSessionAsync(user, cancellationToken, rotated.Value.NewPlainRefreshToken).ConfigureAwait(false);
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
        var permissions = await permissionResolver.GetPermissionsForRolesAsync(roles, cancellationToken).ConfigureAwait(false);
        var requiresProfile = await torcedorAccount.RequiresProfileCompletionAsync(user.Id, cancellationToken).ConfigureAwait(false);
        return new MeResponse(user.Id, user.Email ?? string.Empty, user.Name, roles.ToList(), permissions, requiresProfile);
    }

    public async Task<AuthResponse?> AcceptStaffInviteAsync(
        string token,
        string password,
        string? name,
        CancellationToken cancellationToken = default)
    {
        var accepted = await staffAdministration.AcceptInviteAsync(token, password, name, cancellationToken).ConfigureAwait(false);
        if (accepted is null)
            return null;

        var user = await userManager.FindByIdAsync(accepted.UserId.ToString()).ConfigureAwait(false);
        if (user is null || !user.IsActive)
            return null;

        return await IssueSessionAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AuthResponse?> IssueSessionForUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null || !user.IsActive)
            return null;
        return await IssueSessionAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AuthResponse?> SignInWithGoogleAsync(
        GoogleSignInRequest request,
        CancellationToken cancellationToken = default)
    {
        var validated = await googleTokens.ValidateAsync(request.IdToken, cancellationToken).ConfigureAwait(false);
        if (validated is null)
            return null;

        var user = await userManager.FindByLoginAsync("Google", validated.Subject).ConfigureAwait(false);
        if (user is null)
            user = await userManager.FindByEmailAsync(validated.Email).ConfigureAwait(false);

        if (user is not null)
        {
            if (!user.IsActive)
                return null;
            var logins = await userManager.GetLoginsAsync(user).ConfigureAwait(false);
            if (!logins.Any(l => l.LoginProvider == "Google" && l.ProviderKey == validated.Subject))
            {
                var addLogin = await userManager.AddLoginAsync(user, new UserLoginInfo("Google", validated.Subject, "Google"))
                    .ConfigureAwait(false);
                if (!addLogin.Succeeded)
                    return null;
            }

            return await IssueSessionAsync(user, cancellationToken).ConfigureAwait(false);
        }

        // New user: LGPD consents required (same as public register).
        var consents = request.AcceptedLegalDocumentVersionIds ?? [];
        if (consents.Count == 0)
            return null;

        var displayName = string.IsNullOrWhiteSpace(validated.Name) ? validated.Email.Split('@')[0] : validated.Name.Trim();
        var registered = await torcedorAccount.RegisterGoogleUserAsync(
            Guid.NewGuid(),
            validated.Email,
            displayName,
            validated.EmailVerified,
            validated.Subject,
            consents,
            cancellationToken).ConfigureAwait(false);

        if (!registered.Succeeded)
            return null;

        user = await userManager.FindByIdAsync(registered.UserId!.Value.ToString()).ConfigureAwait(false);
        if (user is null || !user.IsActive)
            return null;

        return await IssueSessionAsync(user, cancellationToken).ConfigureAwait(false);
    }

    private async Task<AuthResponse?> IssueSessionAsync(
        ApplicationUser user,
        CancellationToken cancellationToken,
        string? existingRefreshPlain = null)
    {
        var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        var permissions = await permissionResolver.GetPermissionsForRolesAsync(roles, cancellationToken).ConfigureAwait(false);
        var (access, expiresIn) = jwt.IssueAccessToken(user, roles, permissions);
        var refreshLifetime = TimeSpan.FromDays(jwtOptions.Value.RefreshTokenDays);
        var refresh = existingRefreshPlain is not null
            ? existingRefreshPlain
            : (await refreshTokens.CreateAsync(user.Id, refreshLifetime, cancellationToken).ConfigureAwait(false));
        return new AuthResponse(access, refresh, expiresIn, roles.ToList());
    }
}
