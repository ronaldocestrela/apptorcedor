using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Persistence;

public sealed class PaymentsMasterDbContextFactory : IDesignTimeDbContextFactory<PaymentsMasterDbContext>
{
    public PaymentsMasterDbContext CreateDbContext(string[] args)
    {
        var basePath = DesignTimeHostResolver.ResolveHostApiDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = configuration.GetConnectionString("MasterDb")
            ?? throw new InvalidOperationException("Connection string 'MasterDb' is not configured.");

        var options = new DbContextOptionsBuilder<PaymentsMasterDbContext>()
            .UseSqlServer(cs, sql => sql.MigrationsHistoryTable("__EFPaymentsMasterMigrationsHistory"))
            .Options;

        return new PaymentsMasterDbContext(options);
    }
}
