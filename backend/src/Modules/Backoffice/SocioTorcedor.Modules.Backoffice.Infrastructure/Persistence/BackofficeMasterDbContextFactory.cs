using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SocioTorcedor.Modules.Backoffice.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for <c>dotnet ef</c> (startup project = Host).
/// </summary>
public sealed class BackofficeMasterDbContextFactory : IDesignTimeDbContextFactory<BackofficeMasterDbContext>
{
    public BackofficeMasterDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("MasterDb")
            ?? throw new InvalidOperationException(
                "Connection string 'MasterDb' not found. Run migrations with --startup-project pointing to SocioTorcedor.Api.");

        var optionsBuilder = new DbContextOptionsBuilder<BackofficeMasterDbContext>();
        optionsBuilder.UseSqlServer(
            connectionString,
            sql => sql.MigrationsHistoryTable("__EFBackofficeMigrationsHistory"));
        return new BackofficeMasterDbContext(optionsBuilder.Options);
    }
}
