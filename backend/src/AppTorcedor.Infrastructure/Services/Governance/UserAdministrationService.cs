using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Account;
using AppTorcedor.Application.Validation;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Governance;

public sealed class UserAdministrationService(
    AppDbContext db,
    UserManager<ApplicationUser> userManager) : IUserAdministrationPort
{
    public async Task<AdminUserListPageDto> ListUsersAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Users.AsNoTracking().AsQueryable();
        if (isActive is { } active)
            query = query.Where(u => u.IsActive == active);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var termLower = term.ToLowerInvariant();
            var profileUserIds = db.UserProfiles.AsNoTracking()
                .Where(p => p.Document != null && p.Document.ToLower().Contains(termLower))
                .Select(p => p.UserId);
            query = query.Where(
                u =>
                    (u.Email != null && u.Email.ToLower().Contains(termLower))
                    || u.Name.ToLower().Contains(termLower)
                    || profileUserIds.Contains(u.Id));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var pageQuery = query.OrderBy(u => u.Email).Skip((page - 1) * pageSize).Take(pageSize);
        var users = await pageQuery.ToListAsync(cancellationToken).ConfigureAwait(false);
        if (users.Count == 0)
            return new AdminUserListPageDto(total, []);

        var ids = users.Select(u => u.Id).ToList();
        var profiles = await db.UserProfiles.AsNoTracking()
            .Where(p => ids.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, cancellationToken)
            .ConfigureAwait(false);
        var memberships = await db.Memberships.AsNoTracking()
            .Where(m => ids.Contains(m.UserId))
            .ToDictionaryAsync(m => m.UserId, cancellationToken)
            .ConfigureAwait(false);

        var items = new List<AdminUserListItemDto>();
        foreach (var u in users)
        {
            var fullUser = await userManager.FindByIdAsync(u.Id.ToString()).ConfigureAwait(false);
            if (fullUser is null)
                continue;
            var roles = await userManager.GetRolesAsync(fullUser).ConfigureAwait(false);
            var isStaff = roles.Any(SystemRoles.IsAssignableStaffRole);
            profiles.TryGetValue(u.Id, out var prof);
            memberships.TryGetValue(u.Id, out var mem);
            items.Add(
                new AdminUserListItemDto(
                    fullUser.Id,
                    fullUser.Email ?? string.Empty,
                    fullUser.Name,
                    fullUser.IsActive,
                    fullUser.CreatedAt,
                    isStaff,
                    mem?.Status.ToString(),
                    prof?.Document));
        }

        return new AdminUserListPageDto(total, items);
    }

    public async Task<AdminUserDetailDto?> GetUserDetailAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
            return null;

        var roles = (await userManager.GetRolesAsync(user).ConfigureAwait(false)).OrderBy(r => r).ToList();
        var isStaff = roles.Any(SystemRoles.IsAssignableStaffRole);

        var profileEntity = await db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken).ConfigureAwait(false);
        AdminUserProfileDto? profile = profileEntity is null
            ? null
            : new AdminUserProfileDto(
                profileEntity.Document,
                profileEntity.BirthDate,
                profileEntity.PhotoUrl,
                profileEntity.Address,
                profileEntity.AdministrativeNote);

        var membershipEntity = await db.Memberships.AsNoTracking().FirstOrDefaultAsync(m => m.UserId == userId, cancellationToken).ConfigureAwait(false);
        AdminUserMembershipSummaryDto? membership = membershipEntity is null
            ? null
            : new AdminUserMembershipSummaryDto(
                membershipEntity.Id,
                membershipEntity.Status.ToString(),
                membershipEntity.PlanId,
                membershipEntity.StartDate,
                membershipEntity.EndDate,
                membershipEntity.NextDueDate);

        return new AdminUserDetailDto(
            user.Id,
            user.Email ?? string.Empty,
            user.Name,
            user.PhoneNumber,
            user.IsActive,
            user.CreatedAt,
            isStaff,
            roles,
            profile,
            membership);
    }

    public async Task<bool> SetAccountActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
            return false;

        user.IsActive = isActive;
        var update = await userManager.UpdateAsync(user).ConfigureAwait(false);
        return update.Succeeded;
    }

    public async Task<ProfileUpsertResult> UpsertProfileAsync(
        Guid userId,
        AdminUserProfileUpsertDto patch,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId, cancellationToken).ConfigureAwait(false))
            return ProfileUpsertResult.NotFound();

        var entity = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            entity = new UserProfileRecord { UserId = userId };
            db.UserProfiles.Add(entity);
        }

        if (patch.Document is not null)
        {
            if (string.IsNullOrWhiteSpace(patch.Document))
                entity.Document = null;
            else if (!CpfNumber.TryParse(patch.Document, out var normalized))
                return ProfileUpsertResult.InvalidDocument();
            else
            {
                var taken = await db.UserProfiles
                    .AsNoTracking()
                    .AnyAsync(p => p.Document == normalized && p.UserId != userId, cancellationToken)
                    .ConfigureAwait(false);
                if (taken)
                    return ProfileUpsertResult.DocumentAlreadyInUse();
                entity.Document = normalized;
            }
        }

        if (patch.BirthDate is not null)
            entity.BirthDate = patch.BirthDate;
        if (patch.PhotoUrl is not null)
            entity.PhotoUrl = string.IsNullOrWhiteSpace(patch.PhotoUrl) ? null : patch.PhotoUrl.Trim();
        if (patch.Address is not null)
            entity.Address = string.IsNullOrWhiteSpace(patch.Address) ? null : patch.Address.Trim();
        if (patch.AdministrativeNote is not null)
            entity.AdministrativeNote = string.IsNullOrWhiteSpace(patch.AdministrativeNote) ? null : patch.AdministrativeNote.Trim();

        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (SqlServerUniqueIndexViolation.IsUniqueConstraintOnSave(ex))
        {
            return ProfileUpsertResult.DocumentAlreadyInUse();
        }

        return ProfileUpsertResult.Ok();
    }
}
