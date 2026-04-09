using Microsoft.EntityFrameworkCore;
using SocioTorcedor.Modules.Identity.Infrastructure.Persistence;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;

namespace SocioTorcedor.Modules.Identity.Infrastructure.Services;

public sealed class TenantDatabaseProvisioner : ITenantDatabaseProvisioner
{
    public async Task ProvisionAsync(string connectionString, CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<TenantIdentityDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var db = new TenantIdentityDbContext(options);
        await db.Database.MigrateAsync(cancellationToken);
    }
}
