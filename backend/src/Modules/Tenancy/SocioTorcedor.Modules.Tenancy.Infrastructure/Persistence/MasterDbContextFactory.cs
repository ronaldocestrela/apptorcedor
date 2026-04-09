using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SocioTorcedor.Modules.Tenancy.Infrastructure.Persistence;

/// <summary>
/// Usado apenas por <c>dotnet ef</c> (startup project = Host). O diretório de trabalho é o do Host, onde estão appsettings.
/// </summary>
public sealed class MasterDbContextFactory : IDesignTimeDbContextFactory<MasterDbContext>
{
    public MasterDbContext CreateDbContext(string[] args)
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

        var optionsBuilder = new DbContextOptionsBuilder<MasterDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new MasterDbContext(optionsBuilder.Options);
    }
}
