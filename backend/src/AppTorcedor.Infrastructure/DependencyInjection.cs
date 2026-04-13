using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Auditing;
using AppTorcedor.Infrastructure.Persistence;
using AppTorcedor.Infrastructure.Services;
using AppTorcedor.Infrastructure.Services.Governance;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppTorcedor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<CurrentAuditContext>();
        services.AddScoped<ICurrentAuditContext>(sp => sp.GetRequiredService<CurrentAuditContext>());
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>(
            (sp, options) =>
            {
                var useInMemory = configuration.GetValue<bool>("UseInMemoryDatabase");
                if (useInMemory)
                {
                    var inMemoryName = configuration["Testing:InMemoryDatabaseName"] ?? "AppTorcedor";
                    options.UseInMemoryDatabase(inMemoryName);
                }
                else
                {
                    var cs = configuration.GetConnectionString("DefaultConnection")
                        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
                    options.UseSqlServer(cs);
                }

                options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
            });

        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
        services.AddScoped<IDatabaseConnectivityCheck, DatabaseConnectivityCheck>();
        services.AddScoped<IPermissionResolver, PermissionResolver>();
        services.AddScoped<IMembershipWritePort, MembershipWritePort>();
        services.AddScoped<IAppConfigurationPort, AppConfigurationPort>();
        services.AddScoped<IRolePermissionReadPort, RolePermissionReadPort>();
        services.AddScoped<IRolePermissionWritePort, RolePermissionWritePort>();
        services.AddScoped<IStaffAdministrationPort, StaffAdministrationService>();
        services.AddScoped<IAdminDashboardReadPort, AdminDashboardReadPort>();
        services.AddScoped<IAuditLogReadPort, AuditLogReadPort>();

        return services;
    }
}
