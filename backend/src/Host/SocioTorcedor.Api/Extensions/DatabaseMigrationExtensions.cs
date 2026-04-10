using Microsoft.EntityFrameworkCore;
using SocioTorcedor.Modules.Backoffice.Infrastructure.Persistence;
using SocioTorcedor.Modules.Identity.Infrastructure.Persistence;
using SocioTorcedor.Modules.Membership.Infrastructure.Persistence;
using SocioTorcedor.Modules.Payments.Infrastructure.Persistence;
using SocioTorcedor.Modules.Tenancy.Infrastructure.Persistence;

namespace SocioTorcedor.Api.Extensions;

/// <summary>
/// Ao subir a API, aplica todas as migrations pendentes do EF Core (em ordem) no banco master e,
/// em seguida, em cada connection string distinta cadastrada em <c>Tenants</c> (Identity por tenant).
/// </summary>
public static class DatabaseMigrationExtensions
{
    public static async Task ApplyPendingEfCoreMigrationsAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        var enabled = app.Configuration.GetValue("Database:ApplyMigrationsAtStartup", true);
        if (!enabled)
        {
            app.Logger.LogInformation(
                "Database migrations at startup are disabled (Database:ApplyMigrationsAtStartup=false).");
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var master = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

        app.Logger.LogInformation("Applying pending EF Core migrations to master database...");
        await master.Database.MigrateAsync(cancellationToken);

        var backoffice = scope.ServiceProvider.GetRequiredService<BackofficeMasterDbContext>();
        app.Logger.LogInformation("Applying pending EF Core migrations to backoffice (master) tables...");
        await backoffice.Database.MigrateAsync(cancellationToken);

        var paymentsMaster = scope.ServiceProvider.GetRequiredService<PaymentsMasterDbContext>();
        app.Logger.LogInformation("Applying pending EF Core migrations to payments (master) tables...");
        await paymentsMaster.Database.MigrateAsync(cancellationToken);

        var tenantConnectionStrings = await master.Tenants
            .AsNoTracking()
            .Where(t => !string.IsNullOrWhiteSpace(t.ConnectionString))
            .Select(t => t.ConnectionString)
            .Distinct()
            .ToListAsync(cancellationToken);

        app.Logger.LogInformation(
            "Applying pending EF Core migrations to {TenantCount} tenant database(s)...",
            tenantConnectionStrings.Count);

        foreach (var connectionString in tenantConnectionStrings)
        {
            var identityOptions = new DbContextOptionsBuilder<TenantIdentityDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            await using (var tenantIdentityDb = new TenantIdentityDbContext(identityOptions))
            {
                await tenantIdentityDb.Database.MigrateAsync(cancellationToken);
                await LegalDocumentTenantSeed.SeedIfEmptyAsync(tenantIdentityDb, cancellationToken);
                await RoleTenantSeed.SeedAsync(tenantIdentityDb, cancellationToken);
            }

            var membershipOptions = new DbContextOptionsBuilder<TenantMembershipDbContext>()
                .UseSqlServer(
                    connectionString,
                    o => o.MigrationsHistoryTable("__EFMembershipMigrationsHistory"))
                .Options;

            await using var tenantMembershipDb = new TenantMembershipDbContext(membershipOptions);
            await tenantMembershipDb.Database.MigrateAsync(cancellationToken);

            var paymentsTenantOptions = new DbContextOptionsBuilder<TenantPaymentsDbContext>()
                .UseSqlServer(
                    connectionString,
                    o => o.MigrationsHistoryTable("__EFPaymentsTenantMigrationsHistory"))
                .Options;

            await using var tenantPaymentsDb = new TenantPaymentsDbContext(paymentsTenantOptions);
            await tenantPaymentsDb.Database.MigrateAsync(cancellationToken);
        }

        app.Logger.LogInformation("EF Core migrations applied (master + tenant databases).");
    }
}
