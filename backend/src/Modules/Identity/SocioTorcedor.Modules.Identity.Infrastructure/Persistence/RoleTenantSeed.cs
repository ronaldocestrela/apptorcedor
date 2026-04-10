using Microsoft.EntityFrameworkCore;
using SocioTorcedor.Modules.Identity.Infrastructure.Entities;

namespace SocioTorcedor.Modules.Identity.Infrastructure.Persistence;

public static class RoleTenantSeed
{
    public const string SocioRoleName = "Socio";
    public const string AdministradorRoleName = "Administrador";

    private static readonly (string Name, string Description)[] RolesToEnsure =
    [
        (SocioRoleName, "Default member role"),
        (AdministradorRoleName, "Tenant administrator role")
    ];

    public static async Task SeedAsync(TenantIdentityDbContext db, CancellationToken cancellationToken)
    {
        var existingNormalized = await db.Roles.AsNoTracking()
            .Where(r => r.NormalizedName != null)
            .Select(r => r.NormalizedName!)
            .ToHashSetAsync(cancellationToken);

        var added = false;
        foreach (var (name, description) in RolesToEnsure)
        {
            var normalized = name.ToUpperInvariant();
            if (existingNormalized.Contains(normalized))
                continue;

            db.Roles.Add(new ApplicationRole
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                NormalizedName = normalized,
                Description = description,
                ConcurrencyStamp = Guid.NewGuid().ToString()
            });
            existingNormalized.Add(normalized);
            added = true;
        }

        if (added)
            await db.SaveChangesAsync(cancellationToken);
    }
}
