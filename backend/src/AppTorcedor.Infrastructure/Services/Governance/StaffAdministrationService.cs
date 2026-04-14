using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppTorcedor.Infrastructure.Services.Governance;

public sealed class StaffAdministrationService(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    ILookupNormalizer lookupNormalizer,
    ILogger<StaffAdministrationService> log) : IStaffAdministrationPort
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<StaffInviteCreatedDto> CreateInviteAsync(
        string email,
        string name,
        IReadOnlyList<string> roles,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateStaffRoles(roles);

        var trimmedEmail = email.Trim();
        var normalized = lookupNormalizer.NormalizeEmail(trimmedEmail);
        if (string.IsNullOrEmpty(normalized))
            throw new InvalidOperationException("Email is required.");

        var now = DateTimeOffset.UtcNow;
        if (await db.StaffInvites.AnyAsync(
                i => i.NormalizedEmail == normalized && i.ConsumedAt == null && i.ExpiresAt > now,
                cancellationToken)
            .ConfigureAwait(false))
            throw new InvalidOperationException("A pending invite already exists for this email.");

        if (await userManager.FindByEmailAsync(trimmedEmail).ConfigureAwait(false) is not null)
            throw new InvalidOperationException("A user with this email already exists.");

        var plainToken = GenerateSecureToken();
        var hash = HashToken(plainToken);
        var id = Guid.NewGuid();
        var expires = now.AddDays(7);

        db.StaffInvites.Add(
            new StaffInviteRecord
            {
                Id = id,
                Email = trimmedEmail,
                NormalizedEmail = normalized,
                Name = name.Trim(),
                TokenHash = hash,
                RolesJson = JsonSerializer.Serialize(roles.ToList(), JsonOptions),
                CreatedAt = now,
                ExpiresAt = expires,
                CreatedByUserId = createdByUserId,
            });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new StaffInviteCreatedDto(id, plainToken, expires);
    }

    public async Task<IReadOnlyList<StaffInviteRowDto>> ListPendingInvitesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var rows = await db.StaffInvites.AsNoTracking()
            .Where(i => i.ConsumedAt == null && i.ExpiresAt > now)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(ToInviteRow).ToList();
    }

    public async Task<IReadOnlyList<StaffUserRowDto>> ListStaffUsersAsync(CancellationToken cancellationToken = default)
    {
        var torcedorRole = await db.Roles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == SystemRoles.Torcedor, cancellationToken)
            .ConfigureAwait(false);
        if (torcedorRole is null)
            return [];

        var staffUserIds = await db.UserRoles.AsNoTracking()
            .Where(ur => ur.RoleId != torcedorRole.Id)
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (staffUserIds.Count == 0)
            return [];

        var users = await db.Users.AsNoTracking()
            .Where(u => staffUserIds.Contains(u.Id))
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = new List<StaffUserRowDto>();
        foreach (var u in users)
        {
            var fullUser = await userManager.FindByIdAsync(u.Id.ToString()).ConfigureAwait(false);
            if (fullUser is null)
                continue;
            var roleNames = await userManager.GetRolesAsync(fullUser).ConfigureAwait(false);
            result.Add(
                new StaffUserRowDto(
                    fullUser.Id,
                    fullUser.Email ?? string.Empty,
                    fullUser.Name,
                    fullUser.IsActive,
                    roleNames.OrderBy(r => r).ToList()));
        }

        return result;
    }

    public async Task<bool> SetUserActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null || !await IsStaffUserAsync(user).ConfigureAwait(false))
            return false;

        user.IsActive = isActive;
        var update = await userManager.UpdateAsync(user).ConfigureAwait(false);
        if (!update.Succeeded)
        {
            log.LogWarning("Failed to update user {UserId}: {Errors}", userId, string.Join(", ", update.Errors.Select(e => e.Description)));
            return false;
        }

        return true;
    }

    public async Task<bool> ReplaceUserRolesAsync(Guid userId, IReadOnlyList<string> roles, CancellationToken cancellationToken = default)
    {
        ValidateStaffRoles(roles);

        var user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null || !await IsStaffUserAsync(user).ConfigureAwait(false))
            return false;

        var current = (await userManager.GetRolesAsync(user).ConfigureAwait(false)).ToList();
        if (current.Count > 0)
        {
            var remove = await userManager.RemoveFromRolesAsync(user, current).ConfigureAwait(false);
            if (!remove.Succeeded)
                throw new InvalidOperationException(string.Join(", ", remove.Errors.Select(e => e.Description)));
        }

        var add = await userManager.AddToRolesAsync(user, roles).ConfigureAwait(false);
        if (!add.Succeeded)
            throw new InvalidOperationException(string.Join(", ", add.Errors.Select(e => e.Description)));

        return true;
    }

    public async Task<AcceptStaffInviteResultDto?> AcceptInviteAsync(
        string plainToken,
        string password,
        string? nameOverride,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(plainToken))
            return null;

        var hash = HashToken(plainToken.Trim());
        var now = DateTimeOffset.UtcNow;
        var invite = await db.StaffInvites.FirstOrDefaultAsync(
                i => i.TokenHash == hash && i.ConsumedAt == null && i.ExpiresAt > now,
                cancellationToken)
            .ConfigureAwait(false);
        if (invite is null)
            return null;

        if (await userManager.FindByEmailAsync(invite.Email).ConfigureAwait(false) is not null)
            return null;

        List<string> roles;
        try
        {
            roles = JsonSerializer.Deserialize<List<string>>(invite.RolesJson, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return null;
        }

        ValidateStaffRoles(roles);

        var displayName = string.IsNullOrWhiteSpace(nameOverride) ? invite.Name : nameOverride.Trim();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = invite.Email,
            Email = invite.Email,
            EmailConfirmed = true,
            Name = displayName,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var create = await userManager.CreateAsync(user, password).ConfigureAwait(false);
        if (!create.Succeeded)
        {
            log.LogWarning("Accept invite failed creating user: {Errors}", string.Join(", ", create.Errors.Select(e => e.Description)));
            return null;
        }

        var addRoles = await userManager.AddToRolesAsync(user, roles).ConfigureAwait(false);
        if (!addRoles.Succeeded)
        {
            await userManager.DeleteAsync(user).ConfigureAwait(false);
            log.LogWarning("Accept invite failed roles: {Errors}", string.Join(", ", addRoles.Errors.Select(e => e.Description)));
            return null;
        }

        invite.ConsumedAt = now;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new AcceptStaffInviteResultDto(user.Id, roles);
    }

    private async Task<bool> IsStaffUserAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        return roles.Any(r => SystemRoles.IsAssignableStaffRole(r));
    }

    private static void ValidateStaffRoles(IReadOnlyList<string> roles)
    {
        if (roles.Count == 0)
            throw new InvalidOperationException("At least one staff role is required.");

        foreach (var r in roles)
        {
            if (!SystemRoles.IsAssignableStaffRole(r))
                throw new InvalidOperationException($"Role '{r}' cannot be assigned to staff.");
        }
    }

    private static StaffInviteRowDto ToInviteRow(StaffInviteRecord i)
    {
        List<string> roles;
        try
        {
            roles = JsonSerializer.Deserialize<List<string>>(i.RolesJson, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            roles = [];
        }

        return new StaffInviteRowDto(i.Id, i.Email, i.Name, roles, i.CreatedAt, i.ExpiresAt);
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string plain)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plain));
        return Convert.ToBase64String(bytes);
    }
}
