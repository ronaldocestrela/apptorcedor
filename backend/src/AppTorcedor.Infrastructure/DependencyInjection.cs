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
using AppTorcedor.Infrastructure.Services.Membership;
using AppTorcedor.Infrastructure.Services.News;
using AppTorcedor.Infrastructure.Services.Plans;
using AppTorcedor.Infrastructure.Services.Support;
using AppTorcedor.Infrastructure.Services.Cors;
using AppTorcedor.Infrastructure.Services.Email;
using AppTorcedor.Infrastructure.Options;
using Resend;
using AppTorcedor.Infrastructure.Services.Account;
using AppTorcedor.Infrastructure.Services.Branding;
using AppTorcedor.Infrastructure.Services.Cloudinary;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppTorcedor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICorsAllowlist, CorsAllowlist>();

        services.Configure<ProfilePhotoStorageOptions>(configuration.GetSection(ProfilePhotoStorageOptions.SectionName));
        services.Configure<TeamShieldStorageOptions>(configuration.GetSection(TeamShieldStorageOptions.SectionName));
        services.Configure<OpponentLogoStorageOptions>(configuration.GetSection(OpponentLogoStorageOptions.SectionName));
        services.Configure<BenefitOfferBannerStorageOptions>(configuration.GetSection(BenefitOfferBannerStorageOptions.SectionName));
        services.Configure<CloudinaryOptions>(configuration.GetSection(CloudinaryOptions.SectionName));
        services.Configure<PaymentsOptions>(configuration.GetSection(PaymentsOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        var emailProvider = configuration.GetValue<string>("Email:Provider") ?? "Mock";
        if (string.Equals(emailProvider.Trim(), "Resend", StringComparison.OrdinalIgnoreCase))
        {
            var apiKey = configuration["Email:Resend:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Email:Resend:ApiKey is required when Email:Provider is Resend.");

            services.AddHttpClient<ResendClient>();
            services.Configure<ResendClientOptions>(options => options.ApiToken = apiKey);
            services.AddTransient<IResend, ResendClient>();
            services.AddScoped<IEmailSender, ResendEmailSender>();
        }
        else
            services.AddScoped<IEmailSender, MockEmailSender>();

        var paymentsProvider = configuration.GetValue<string>("Payments:Provider") ?? "Mock";
        if (string.Equals(paymentsProvider.Trim(), "Stripe", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IPaymentProvider, StripePaymentProvider>();
        else
            services.AddScoped<IPaymentProvider, MockPaymentProvider>();

        services.AddScoped<IStripeWebhookProcessor, StripeWebhookProcessor>();
        services.Configure<SupportTicketAttachmentStorageOptions>(
            configuration.GetSection(SupportTicketAttachmentStorageOptions.SectionName));
        services.AddHttpClient<ICloudinaryAssetGateway, CloudinaryAssetGateway>();
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
                    // InMemory não suporta transações; ignoramos o warning para permitir o mesmo fluxo atômico
                    // usado em SQL Server (teste de rollback real fica em TorcedorAccountServiceRegisterTests com SQLite).
                    options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
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
        services.AddScoped<IRegistrationLegalReadPort, RegistrationLegalReadService>();
        services.AddScoped<ITorcedorAccountPort, TorcedorAccountService>();
        services.AddScoped<IProfilePhotoStorage>(
            sp =>
            {
                var profileOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ProfilePhotoStorageOptions>>().Value;
                if (profileOptions.Provider.Equals("Cloudinary", StringComparison.OrdinalIgnoreCase))
                    return sp.GetRequiredService<CloudinaryProfilePhotoStorage>();
                return sp.GetRequiredService<LocalProfilePhotoStorage>();
            });
        services.AddScoped<LocalProfilePhotoStorage>();
        services.AddScoped<CloudinaryProfilePhotoStorage>();
        services.AddScoped<ITeamShieldStorage>(
            sp =>
            {
                var shieldOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TeamShieldStorageOptions>>().Value;
                if (shieldOptions.Provider.Equals("Cloudinary", StringComparison.OrdinalIgnoreCase))
                    return sp.GetRequiredService<CloudinaryTeamShieldStorage>();
                return sp.GetRequiredService<LocalTeamShieldStorage>();
            });
        services.AddScoped<LocalTeamShieldStorage>();
        services.AddScoped<CloudinaryTeamShieldStorage>();
        services.AddScoped<IOpponentLogoStorage>(
            sp =>
            {
                var logoOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpponentLogoStorageOptions>>().Value;
                if (logoOptions.Provider.Equals("Cloudinary", StringComparison.OrdinalIgnoreCase))
                    return sp.GetRequiredService<CloudinaryOpponentLogoStorage>();
                return sp.GetRequiredService<LocalOpponentLogoStorage>();
            });
        services.AddScoped<LocalOpponentLogoStorage>();
        services.AddScoped<CloudinaryOpponentLogoStorage>();
        services.AddScoped<IBenefitOfferBannerStorage>(
            sp =>
            {
                var bannerOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BenefitOfferBannerStorageOptions>>().Value;
                if (bannerOptions.Provider.Equals("Cloudinary", StringComparison.OrdinalIgnoreCase))
                    return sp.GetRequiredService<CloudinaryBenefitOfferBannerStorage>();
                return sp.GetRequiredService<LocalBenefitOfferBannerStorage>();
            });
        services.AddScoped<LocalBenefitOfferBannerStorage>();
        services.AddScoped<CloudinaryBenefitOfferBannerStorage>();
        services.AddScoped<IOpponentLogoLibraryAdminPort, OpponentLogoLibraryAdminService>();
        services.AddScoped<ISupportTicketAttachmentStorage>(
            sp =>
            {
                var attachmentOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SupportTicketAttachmentStorageOptions>>().Value;
                if (attachmentOptions.Provider.Equals("Cloudinary", StringComparison.OrdinalIgnoreCase))
                    return sp.GetRequiredService<CloudinarySupportTicketAttachmentStorage>();
                return sp.GetRequiredService<LocalSupportTicketAttachmentStorage>();
            });
        services.AddScoped<LocalSupportTicketAttachmentStorage>();
        services.AddScoped<CloudinarySupportTicketAttachmentStorage>();
        services.AddScoped<IAdminDashboardReadPort, AdminDashboardReadPort>();
        services.AddScoped<IAuditLogReadPort, AuditLogReadPort>();
        services.AddScoped<ILgpdAdministrationPort, LgpdAdministrationService>();
        services.AddSingleton<ITicketProvider, MockTicketProvider>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IPaymentsAdministrationPort, PaymentAdministrationService>();
        services.AddScoped<IGameAdministrationPort, GameAdministrationService>();
        services.AddScoped<ITicketAdministrationPort, TicketAdministrationService>();
        services.AddScoped<IGameTorcedorReadPort, GameTorcedorReadService>();
        services.AddScoped<ITicketTorcedorPort, TicketTorcedorService>();
        services.AddScoped<IDigitalCardAdministrationPort, DigitalCardAdministrationService>();
        services.AddScoped<IDigitalCardTorcedorPort, DigitalCardTorcedorReadService>();
        services.AddScoped<INewsAdministrationPort, NewsAdministrationService>();
        services.AddScoped<ITorcedorNewsReadPort, TorcedorNewsReadService>();
        services.AddScoped<ITorcedorPublishedPlansReadPort, TorcedorPublishedPlansReadService>();
        services.AddScoped<ITorcedorMembershipSubscriptionPort, TorcedorMembershipSubscriptionService>();
        services.AddScoped<ITorcedorBenefitsReadPort, TorcedorBenefitsReadService>();
        services.AddScoped<ITorcedorBenefitRedemptionPort, TorcedorBenefitRedemptionService>();
        services.AddScoped<ISupportAdministrationPort, SupportAdministrationService>();
        services.AddScoped<ISupportTorcedorPort, SupportTorcedorService>();
        services.AddScoped<LoyaltyAdministrationService>();
        services.AddScoped<ILoyaltyAdministrationPort>(sp => sp.GetRequiredService<LoyaltyAdministrationService>());
        services.AddScoped<ILoyaltyPointsTriggerPort>(sp => sp.GetRequiredService<LoyaltyAdministrationService>());
        services.AddScoped<ILoyaltyTorcedorReadPort, LoyaltyTorcedorReadService>();
        services.AddScoped<IBenefitsAdministrationPort, BenefitsAdministrationService>();
        services.AddScoped<IInAppNotificationDispatchService, InAppNotificationDispatchService>();
        services.AddScoped<IPaymentDelinquencySweep, PaymentDelinquencySweep>();
        services.AddScoped<IMembershipScheduledCancellationEffectiveSweep, MembershipScheduledCancellationEffectiveSweep>();
        services.AddScoped<ITorcedorSubscriptionCheckoutPort, TorcedorSubscriptionCheckoutService>();
        services.AddScoped<ITorcedorSubscriptionSummaryPort, TorcedorSubscriptionSummaryReadService>();
        services.AddScoped<ITorcedorPlanChangePort, TorcedorPlanChangeService>();
        services.AddScoped<ITorcedorMembershipCancellationPort, TorcedorMembershipCancellationService>();
        services.AddHostedService<PaymentDelinquencyHostedService>();
        services.AddHostedService<InAppNotificationDispatchHostedService>();

        return services;
    }
}
