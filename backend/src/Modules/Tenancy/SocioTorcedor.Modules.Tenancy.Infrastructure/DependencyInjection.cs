using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;
using SocioTorcedor.Modules.Tenancy.Infrastructure.Persistence;
using SocioTorcedor.Modules.Tenancy.Infrastructure.Repositories;
using SocioTorcedor.Modules.Tenancy.Infrastructure.Services;

namespace SocioTorcedor.Modules.Tenancy.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTenancyInfrastructure(
        this IServiceCollection services,
        string masterConnectionString)
    {
        services.AddDbContext<MasterDbContext>(options =>
            options.UseSqlServer(masterConnectionString));

        services.AddMemoryCache();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantResolver, TenantSlugResolver>();
        return services;
    }
}
