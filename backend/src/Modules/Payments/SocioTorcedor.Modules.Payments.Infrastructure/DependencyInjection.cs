using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SocioTorcedor.BuildingBlocks.Application.Payments;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.Options;
using SocioTorcedor.Modules.Payments.Application.Services;
using SocioTorcedor.Modules.Payments.Infrastructure.Options;
using SocioTorcedor.Modules.Payments.Infrastructure.Persistence;
using SocioTorcedor.Modules.Payments.Infrastructure.Repositories;
using SocioTorcedor.Modules.Payments.Infrastructure.Services;
using SocioTorcedor.Modules.Tenancy.Infrastructure.Persistence;

namespace SocioTorcedor.Modules.Payments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PaymentsOptions>(configuration.GetSection(PaymentsOptions.SectionName));
        services.Configure<StripeWebhookHandlingOptions>(configuration.GetSection(StripeWebhookHandlingOptions.SectionName));

        var masterConnectionString = configuration.GetConnectionString("MasterDb")
            ?? throw new InvalidOperationException("Connection string 'MasterDb' is not configured.");

        services.AddDbContext<PaymentsMasterDbContext>(options =>
            options.UseSqlServer(
                masterConnectionString,
                sql => sql.MigrationsHistoryTable("__EFPaymentsMasterMigrationsHistory")));

        services.AddDbContext<TenantPaymentsDbContext>((sp, builder) =>
        {
            var tenant = sp.GetRequiredService<ICurrentTenantContext>();
            if (!tenant.IsResolved)
                throw new InvalidOperationException("Tenant must be resolved before accessing tenant payments database.");

            builder.UseSqlServer(
                tenant.TenantConnectionString,
                o => o.MigrationsHistoryTable("__EFPaymentsTenantMigrationsHistory"));
        });

        services.AddScoped<ITenantMasterPaymentsRepository, TenantMasterPaymentsRepository>();
        services.AddScoped<IMemberTenantPaymentsRepository, MemberTenantPaymentsRepository>();
        services.AddScoped<ITenantConnectionStringResolver, TenantConnectionStringResolver>();
        services.AddScoped<IMemberTenantPaymentsScopeFactory, MemberTenantPaymentsScopeFactory>();

        services.AddSingleton(provider =>
        {
            var key = provider.GetRequiredService<IOptions<PaymentsOptions>>().Value.StripeSecretKey;
            return new Stripe.StripeClient(string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim());
        });

        services.AddSingleton<StripePaymentProvider>();
        services.AddScoped<IPaymentProvider, RoutingPaymentProvider>();
        services.AddScoped<MemberStripeOperationsResolver>();
        services.AddSingleton<ITenantMemberGatewayCredentialProtector, TenantMemberGatewayCredentialProtector>();
        services.AddScoped<IMemberPaymentGatewayService, MemberPaymentGatewayService>();
        services.AddScoped<IMemberStripeWebhookIngressResolver, MemberStripeWebhookIngressResolver>();

        services.AddScoped<ITenantSaasStripeWebhookEffectApplicator, TenantSaasStripeWebhookEffectApplicator>();
        services.AddScoped<IMemberStripeWebhookEffectApplicator, MemberStripeWebhookEffectApplicator>();
        services.AddScoped<IStripeThinWebhookPayloadFactory, StripeThinWebhookPayloadFactory>();

        services.AddSingleton<IPaymentsGatewayMetadata, PaymentsGatewayMetadata>();

        return services;
    }
}
