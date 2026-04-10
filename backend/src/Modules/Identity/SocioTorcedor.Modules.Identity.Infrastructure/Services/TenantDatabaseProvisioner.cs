using Microsoft.EntityFrameworkCore;
using SocioTorcedor.Modules.Identity.Infrastructure.Persistence;
using SocioTorcedor.Modules.Membership.Infrastructure.Persistence;
using SocioTorcedor.Modules.Payments.Infrastructure.Persistence;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;

namespace SocioTorcedor.Modules.Identity.Infrastructure.Services;

public sealed class TenantDatabaseProvisioner : ITenantDatabaseProvisioner
{
    public async Task ProvisionAsync(string connectionString, CancellationToken cancellationToken)
    {
        var identityOptions = new DbContextOptionsBuilder<TenantIdentityDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using (var identityDb = new TenantIdentityDbContext(identityOptions))
        {
            await identityDb.Database.MigrateAsync(cancellationToken);
            await LegalDocumentTenantSeed.SeedIfEmptyAsync(identityDb, cancellationToken);
        }

        var membershipOptions = new DbContextOptionsBuilder<TenantMembershipDbContext>()
            .UseSqlServer(
                connectionString,
                o => o.MigrationsHistoryTable("__EFMembershipMigrationsHistory"))
            .Options;

        await using var membershipDb = new TenantMembershipDbContext(membershipOptions);
        await membershipDb.Database.MigrateAsync(cancellationToken);

        var paymentsOptions = new DbContextOptionsBuilder<TenantPaymentsDbContext>()
            .UseSqlServer(
                connectionString,
                o => o.MigrationsHistoryTable("__EFPaymentsTenantMigrationsHistory"))
            .Options;

        await using var paymentsDb = new TenantPaymentsDbContext(paymentsOptions);
        await paymentsDb.Database.MigrateAsync(cancellationToken);
    }
}
