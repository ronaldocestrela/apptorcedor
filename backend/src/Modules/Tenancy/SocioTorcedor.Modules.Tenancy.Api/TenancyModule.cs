using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SocioTorcedor.Modules.Tenancy.Infrastructure;

namespace SocioTorcedor.Modules.Tenancy.Api;

public static class TenancyModule
{
    public static IServiceCollection AddTenancyModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MasterDb")
            ?? throw new InvalidOperationException("Connection string 'MasterDb' is not configured.");

        return services.AddTenancyInfrastructure(connectionString);
    }
}
