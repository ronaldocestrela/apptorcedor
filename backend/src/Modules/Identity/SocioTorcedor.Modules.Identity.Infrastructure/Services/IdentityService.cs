using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Identity.Application.Contracts;
using SocioTorcedor.Modules.Identity.Application.DTOs;
using SocioTorcedor.Modules.Identity.Infrastructure.Entities;
using SocioTorcedor.Modules.Identity.Infrastructure.Persistence;

namespace SocioTorcedor.Modules.Identity.Infrastructure.Services;

public sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    TenantIdentityDbContext db,
    IJwtTokenService jwtTokenService) : IIdentityService
{
    private const string DefaultMemberRole = "Socio";

    public async Task<Result<AuthResultDto>> RegisterAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existing = await userManager.FindByEmailAsync(normalizedEmail);
        if (existing is not null)
            return Result<AuthResultDto>.Fail(Error.Conflict("Identity.DuplicateEmail", "Email is already registered."));

        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            EmailConfirmed = true,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow
        };

        var create = await userManager.CreateAsync(user, password);
        if (!create.Succeeded)
        {
            var msg = string.Join(" ", create.Errors.Select(e => e.Description));
            return Result<AuthResultDto>.Fail(Error.Validation("Identity.CreateUser", msg));
        }

        await EnsureRoleAsync(DefaultMemberRole, cancellationToken);
        await userManager.AddToRoleAsync(user, DefaultMemberRole);

        return await BuildAuthResultAsync(user, cancellationToken);
    }

    public async Task<Result<AuthResultDto>> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(normalizedEmail);
        if (user is null)
            return Result<AuthResultDto>.Fail(Error.Failure("Identity.InvalidCredentials", "Invalid email or password."));

        var valid = await userManager.CheckPasswordAsync(user, password);
        if (!valid)
            return Result<AuthResultDto>.Fail(Error.Failure("Identity.InvalidCredentials", "Invalid email or password."));

        return await BuildAuthResultAsync(user, cancellationToken);
    }

    private async Task EnsureRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        if (await roleManager.RoleExistsAsync(roleName))
            return;

        var role = new ApplicationRole
        {
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant(),
            Description = "Default member role"
        };

        var r = await roleManager.CreateAsync(role);
        if (!r.Succeeded)
            throw new InvalidOperationException(string.Join(" ", r.Errors.Select(e => e.Description)));
    }

    private async Task<Result<AuthResultDto>> BuildAuthResultAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var permissions = await GetPermissionNamesAsync(roles, cancellationToken);
        var (token, expires) = jwtTokenService.CreateAccessToken(
            user.Id,
            user.Email ?? string.Empty,
            user.TenantId,
            roles.ToList(),
            permissions);

        return Result<AuthResultDto>.Ok(new AuthResultDto(token, expires));
    }

    private async Task<IReadOnlyList<string>> GetPermissionNamesAsync(
        IList<string> roleNames,
        CancellationToken cancellationToken)
    {
        if (roleNames.Count == 0)
            return Array.Empty<string>();

        var roleIds = await db.Roles.AsNoTracking()
            .Where(r => r.Name != null && roleNames.Contains(r.Name))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        if (roleIds.Count == 0)
            return Array.Empty<string>();

        return await db.RolePermissions.AsNoTracking()
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Join(
                db.Permissions.AsNoTracking(),
                rp => rp.PermissionId,
                p => p.Id,
                (_, p) => p.Name)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
