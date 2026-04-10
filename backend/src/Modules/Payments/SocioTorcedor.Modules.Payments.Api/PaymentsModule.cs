using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SocioTorcedor.Modules.Payments.Infrastructure;

namespace SocioTorcedor.Modules.Payments.Api;

public static class PaymentsModule
{
    public static IServiceCollection AddPaymentsModule(this IServiceCollection services, IConfiguration configuration) =>
        services.AddPaymentsInfrastructure(configuration);
}
