using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SocioTorcedor.Modules.Backoffice.Application.Contracts;
using SocioTorcedor.Modules.Backoffice.Infrastructure.Persistence;
using SocioTorcedor.Modules.Backoffice.Infrastructure.Repositories;

namespace SocioTorcedor.Modules.Backoffice.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBackofficeInfrastructure(
        this IServiceCollection services,
        string masterConnectionString)
    {
        services.AddDbContext<BackofficeMasterDbContext>(options =>
            options.UseSqlServer(
                masterConnectionString,
                sql => sql.MigrationsHistoryTable("__EFBackofficeMigrationsHistory")));

        services.AddScoped<ISaaSPlanRepository, SaaSPlanRepository>();
        services.AddScoped<ITenantPlanRepository, TenantPlanRepository>();
        return services;
    }
}
