using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SocioTorcedor.Modules.Identity.Infrastructure.Persistence;

/// <summary>
/// Design-time: gera migrations do schema Identity por tenant. Use um banco dedicado (template), não o master.
/// </summary>
public sealed class TenantIdentityDbContextFactory : IDesignTimeDbContextFactory<TenantIdentityDbContext>
{
    public TenantIdentityDbContext CreateDbContext(string[] args)
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
                "Connection string 'TenantDesignTime' not found. Configure it in appsettings (same server as tenants; separate database for migration tooling).");

        var optionsBuilder = new DbContextOptionsBuilder<TenantIdentityDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new TenantIdentityDbContext(optionsBuilder.Options);
    }
}
