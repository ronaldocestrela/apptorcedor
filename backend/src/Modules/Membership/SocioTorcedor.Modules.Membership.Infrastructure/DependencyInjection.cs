using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Infrastructure.Persistence;
using SocioTorcedor.Modules.Membership.Infrastructure.Repositories;
using SocioTorcedor.Modules.Membership.Infrastructure.Services;

namespace SocioTorcedor.Modules.Membership.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMembershipInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<TenantMembershipDbContext>((sp, builder) =>
        {
            var tenant = sp.GetRequiredService<ICurrentTenantContext>();
            if (!tenant.IsResolved)
                throw new InvalidOperationException("Tenant must be resolved before accessing the membership database.");

            builder.UseSqlServer(
                tenant.TenantConnectionString,
                o => o.MigrationsHistoryTable("__EFMembershipMigrationsHistory"));
        });

        services.AddScoped<IMemberProfileRepository, MemberProfileRepository>();
        services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();
        return services;
    }
}
