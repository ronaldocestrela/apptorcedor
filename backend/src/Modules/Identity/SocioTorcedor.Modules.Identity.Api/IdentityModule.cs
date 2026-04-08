using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SocioTorcedor.Modules.Identity.Infrastructure;

namespace SocioTorcedor.Modules.Identity.Api;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration) =>
        services.AddIdentityInfrastructure(configuration);
}
