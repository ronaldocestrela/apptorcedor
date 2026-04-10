using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Persistence;

public sealed class TenantPaymentsDbContextFactory : IDesignTimeDbContextFactory<TenantPaymentsDbContext>
{
    public TenantPaymentsDbContext CreateDbContext(string[] args)
    {
        var basePath = DesignTimeHostResolver.ResolveHostApiDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = configuration.GetConnectionString("TenantDesignTime")
            ?? configuration.GetConnectionString("MasterDb")
            ?? throw new InvalidOperationException("Connection string 'TenantDesignTime' or 'MasterDb' is not configured.");

        var options = new DbContextOptionsBuilder<TenantPaymentsDbContext>()
            .UseSqlServer(cs, sql => sql.MigrationsHistoryTable("__EFPaymentsTenantMigrationsHistory"))
            .Options;

        return new TenantPaymentsDbContext(options);
    }
}
