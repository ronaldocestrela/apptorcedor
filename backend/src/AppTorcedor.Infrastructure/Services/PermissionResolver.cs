using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services;

public sealed class PermissionResolver(AppDbContext db) : IPermissionResolver
{
    public async Task<IReadOnlyList<string>> GetPermissionsForRolesAsync(
        IEnumerable<string> roleNames,
        CancellationToken cancellationToken = default)
    {
        var names = roleNames.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();
        if (names.Count == 0)
            return Array.Empty<string>();

        var roleIds = await db.Roles.AsNoTracking().Where(r => names.Contains(r.Name!)).Select(r => r.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (roleIds.Count == 0)
            return Array.Empty<string>();

        return await db.RolePermissions
            .AsNoTracking()
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Join(db.Permissions.AsNoTracking(), rp => rp.PermissionId, p => p.Id, (_, p) => p.Name)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
