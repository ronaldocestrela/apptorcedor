using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SocioTorcedor.Modules.Membership.Infrastructure.Persistence;

/// <summary>
/// Design-time: migrations for membership tables per tenant DB. Use a dedicated database (see TenantDesignTime).
/// </summary>
public sealed class TenantMembershipDbContextFactory : IDesignTimeDbContextFactory<TenantMembershipDbContext>
{
    public TenantMembershipDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("TenantDesignTime")
            ?? throw new InvalidOperationException(
                "Connection string 'TenantDesignTime' not found. Configure it in appsettings (Host project when running dotnet ef).");

        var optionsBuilder = new DbContextOptionsBuilder<TenantMembershipDbContext>();
        optionsBuilder.UseSqlServer(
            connectionString,
            o => o.MigrationsHistoryTable("__EFMembershipMigrationsHistory"));

        return new TenantMembershipDbContext(optionsBuilder.Options);
    }
}
