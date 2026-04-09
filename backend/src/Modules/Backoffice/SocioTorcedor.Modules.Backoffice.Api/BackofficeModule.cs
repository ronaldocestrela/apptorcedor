using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SocioTorcedor.Modules.Backoffice.Infrastructure;

namespace SocioTorcedor.Modules.Backoffice.Api;

public static class BackofficeModule
{
    public static IServiceCollection AddBackofficeModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MasterDb")
            ?? throw new InvalidOperationException("Connection string 'MasterDb' is not configured.");

        return services.AddBackofficeInfrastructure(connectionString);
    }
}
