using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Governance;

public sealed class RolePermissionReadPort(AppDbContext db) : IRolePermissionReadPort
{
    public async Task<IReadOnlyList<RolePermissionRowDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        // Order on scalar columns before projecting to DTO — EF Core cannot translate OrderBy on a record/DTO constructor.
        return await db.RolePermissions.AsNoTracking()
            .Join(db.Roles.AsNoTracking(), rp => rp.RoleId, r => r.Id, (rp, r) => new { rp, RoleName = r.Name })
            .Join(db.Permissions.AsNoTracking(), x => x.rp.PermissionId, p => p.Id, (x, p) => new { x.RoleName, PermissionName = p.Name })
            .OrderBy(x => x.RoleName)
            .ThenBy(x => x.PermissionName)
            .Select(x => new RolePermissionRowDto(x.RoleName ?? string.Empty, x.PermissionName))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
