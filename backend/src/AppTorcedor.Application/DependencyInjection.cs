using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace AppTorcedor.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetAssembly(typeof(AssemblyMarker))!));
        return services;
    }
}
