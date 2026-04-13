using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Auditing;
using AppTorcedor.Infrastructure.Persistence;
using AppTorcedor.Infrastructure.Services;
using AppTorcedor.Infrastructure.Services.Governance;
using AppTorcedor.Infrastructure.Services.Lgpd;
using AppTorcedor.Infrastructure.Services.DigitalCards;
using AppTorcedor.Infrastructure.Services.Games;
using AppTorcedor.Infrastructure.Services.Payments;
using AppTorcedor.Infrastructure.Services.Tickets;
using AppTorcedor.Infrastructure.Services.Loyalty;
using AppTorcedor.Infrastructure.Services.Benefits;
using AppTorcedor.Infrastructure.Services.News;
using AppTorcedor.Infrastructure.Services.Support;
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
        services.AddScoped<IMembershipAdministrationPort, MembershipAdministrationService>();
        services.AddScoped<IPlansAdministrationPort, PlanAdministrationService>();
        services.AddScoped<IAppConfigurationPort, AppConfigurationPort>();
        services.AddScoped<IRolePermissionReadPort, RolePermissionReadPort>();
        services.AddScoped<IRolePermissionWritePort, RolePermissionWritePort>();
        services.AddScoped<IStaffAdministrationPort, StaffAdministrationService>();
        services.AddScoped<IUserAdministrationPort, UserAdministrationService>();
        services.AddScoped<IAdminDashboardReadPort, AdminDashboardReadPort>();
        services.AddScoped<IAuditLogReadPort, AuditLogReadPort>();
        services.AddScoped<ILgpdAdministrationPort, LgpdAdministrationService>();
        services.AddSingleton<ITicketProvider, MockTicketProvider>();
        services.AddScoped<IPaymentProvider, MockPaymentProvider>();
        services.AddScoped<IPaymentsAdministrationPort, PaymentAdministrationService>();
        services.AddScoped<IGameAdministrationPort, GameAdministrationService>();
        services.AddScoped<ITicketAdministrationPort, TicketAdministrationService>();
        services.AddScoped<IDigitalCardAdministrationPort, DigitalCardAdministrationService>();
        services.AddScoped<INewsAdministrationPort, NewsAdministrationService>();
        services.AddScoped<ISupportAdministrationPort, SupportAdministrationService>();
        services.AddScoped<LoyaltyAdministrationService>();
        services.AddScoped<ILoyaltyAdministrationPort>(sp => sp.GetRequiredService<LoyaltyAdministrationService>());
        services.AddScoped<ILoyaltyPointsTriggerPort>(sp => sp.GetRequiredService<LoyaltyAdministrationService>());
        services.AddScoped<IBenefitsAdministrationPort, BenefitsAdministrationService>();
        services.AddScoped<IInAppNotificationDispatchService, InAppNotificationDispatchService>();
        services.AddScoped<IPaymentDelinquencySweep, PaymentDelinquencySweep>();
        services.AddHostedService<PaymentDelinquencyHostedService>();
        services.AddHostedService<InAppNotificationDispatchHostedService>();

        return services;
    }
}
