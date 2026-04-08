using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SocioTorcedor.BuildingBlocks.Application.Behaviors;

namespace SocioTorcedor.BuildingBlocks.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddBuildingBlocksApplication(this IServiceCollection services, params Assembly[] assemblies)
    {
        var scanAssemblies = assemblies.Length > 0
            ? assemblies
            : new[] { Assembly.GetExecutingAssembly() };

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(scanAssemblies));
        services.AddValidatorsFromAssemblies(scanAssemblies);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }
}
