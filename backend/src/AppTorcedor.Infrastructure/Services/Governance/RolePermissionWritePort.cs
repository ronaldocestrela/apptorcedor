using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Governance;

public sealed class RolePermissionWritePort(AppDbContext db) : IRolePermissionWritePort
{
    public async Task ReplaceRolePermissionsAsync(
        string roleName,
        IReadOnlyList<string> permissionNames,
        CancellationToken cancellationToken = default)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken).ConfigureAwait(false);
        if (role is null)
            throw new InvalidOperationException($"Role '{roleName}' was not found.");

        var distinctNames = permissionNames.Distinct(StringComparer.Ordinal).ToList();
        var matches = await db.Permissions.AsNoTracking()
            .Where(p => distinctNames.Contains(p.Name))
            .Select(p => new { p.Id, p.Name })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (matches.Count != distinctNames.Count)
        {
            var missing = distinctNames.Except(matches.Select(m => m.Name), StringComparer.Ordinal).ToList();
            throw new InvalidOperationException($"Unknown permission names: {string.Join(", ", missing)}");
        }

        var existing = await db.RolePermissions.Where(rp => rp.RoleId == role.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        db.RolePermissions.RemoveRange(existing);

        foreach (var m in matches)
            db.RolePermissions.Add(new AppRolePermission { RoleId = role.Id, PermissionId = m.Id });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
