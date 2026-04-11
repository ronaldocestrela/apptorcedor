using Microsoft.OpenApi;
using SocioTorcedor.Api.Swagger;
using SocioTorcedor.BuildingBlocks.Application;
using SocioTorcedor.Modules.Backoffice.Api;
using SocioTorcedor.Modules.Backoffice.Api.Controllers;
using SocioTorcedor.Modules.Backoffice.Application.Commands.AssignPlanToTenant;
using SocioTorcedor.Modules.Backoffice.Application.Commands.CreateSaaSPlan;
using SocioTorcedor.Modules.Identity.Api;
using SocioTorcedor.Modules.Identity.Api.Controllers;
using SocioTorcedor.Modules.Identity.Application.Commands.LoginUser;
using SocioTorcedor.Modules.Identity.Application.Commands.RegisterUser;
using SocioTorcedor.Modules.Membership.Api;
using SocioTorcedor.Modules.Membership.Api.Controllers;
using SocioTorcedor.Modules.Membership.Application.Commands.CreateMemberProfile;
using SocioTorcedor.Modules.Membership.Application.Commands.UpdateMemberProfile;
using SocioTorcedor.Modules.Membership.Application.Queries.GetMemberById;
using SocioTorcedor.Modules.Membership.Application.Queries.GetMyProfile;
using SocioTorcedor.Modules.Membership.Application.Queries.ListMembers;
using SocioTorcedor.Modules.Payments.Api;
using SocioTorcedor.Modules.Payments.Api.Controllers;
using SocioTorcedor.Modules.Payments.Application.Commands.CreateMemberPixCheckout;
using SocioTorcedor.Modules.Payments.Application.Commands.CreateMemberStripeCheckoutSession;
using SocioTorcedor.Modules.Payments.Application.Commands.CreateTenantSaasBillingPortalSession;
using SocioTorcedor.Modules.Payments.Application.Commands.ProcessMemberTenantWebhook;
using SocioTorcedor.Modules.Payments.Application.Commands.ProcessStripeConnectWebhook;
using SocioTorcedor.Modules.Payments.Application.Commands.ProcessTenantSaasWebhook;
using SocioTorcedor.Modules.Payments.Application.Commands.StartStripeConnectOnboarding;
using SocioTorcedor.Modules.Payments.Application.Commands.StartTenantSaasBilling;
using SocioTorcedor.Modules.Payments.Application.Commands.SubscribeMemberPlan;
using SocioTorcedor.Modules.Payments.Application.Queries.GetMyMemberBilling;
using SocioTorcedor.Modules.Payments.Application.Queries.GetStripeConnectStatus;
using SocioTorcedor.Modules.Payments.Application.Queries.GetTenantSaasBilling;
using SocioTorcedor.Modules.Payments.Application.Queries.ListMyMemberInvoices;
using SocioTorcedor.Modules.Payments.Application.Queries.ListTenantSaasInvoices;
using SocioTorcedor.Modules.Tenancy.Api;
using SocioTorcedor.Modules.Tenancy.Application.Commands.CreateTenant;
using SocioTorcedor.Modules.Tenancy.Application.Queries.GetTenantBySlug;

namespace SocioTorcedor.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSocioTorcedorApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(AuthController).Assembly)
            .AddApplicationPart(typeof(TenantsController).Assembly)
            .AddApplicationPart(typeof(MembersController).Assembly)
            .AddApplicationPart(typeof(BackofficeSaasPaymentsController).Assembly)
            .AddApplicationPart(typeof(MemberPaymentsController).Assembly);

        services.AddBuildingBlocksApplication(
            typeof(GetTenantBySlugHandler).Assembly,
            typeof(RegisterUserHandler).Assembly,
            typeof(LoginUserHandler).Assembly,
            typeof(CreateTenantHandler).Assembly,
            typeof(CreateSaaSPlanHandler).Assembly,
            typeof(AssignPlanToTenantHandler).Assembly,
            typeof(CreateMemberProfileHandler).Assembly,
            typeof(UpdateMemberProfileHandler).Assembly,
            typeof(GetMyProfileHandler).Assembly,
            typeof(GetMemberByIdHandler).Assembly,
            typeof(ListMembersHandler).Assembly,
            typeof(StartTenantSaasBillingHandler).Assembly,
            typeof(ProcessTenantSaasWebhookHandler).Assembly,
            typeof(GetTenantSaasBillingHandler).Assembly,
            typeof(ListTenantSaasInvoicesHandler).Assembly,
            typeof(SubscribeMemberPlanHandler).Assembly,
            typeof(CreateMemberPixCheckoutHandler).Assembly,
            typeof(CreateMemberStripeCheckoutSessionHandler).Assembly,
            typeof(CreateTenantSaasBillingPortalSessionHandler).Assembly,
            typeof(StartStripeConnectOnboardingHandler).Assembly,
            typeof(GetStripeConnectStatusHandler).Assembly,
            typeof(ProcessStripeConnectWebhookHandler).Assembly,
            typeof(ProcessMemberTenantWebhookHandler).Assembly,
            typeof(GetMyMemberBillingHandler).Assembly,
            typeof(ListMyMemberInvoicesHandler).Assembly);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Socio Torcedor API",
                Version = "v1",
                Description =
                    "API multitenant: use **X-Tenant-Id** (slug) nas rotas do clube. " +
                    "Rotas **/api/backoffice/** são administrativas (SaaS): não usam tenant; use **X-Api-Key** " +
                    "(configuração `Backoffice:ApiKey`)."
            });

            var bearer = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT para endpoints do tenant (após login). Não aplicável às rotas /api/backoffice/*."
            };

            options.AddSecurityDefinition("Bearer", bearer);

            var backofficeApiKey = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-Api-Key",
                Description =
                    "Chave do backoffice SaaS (mesmo valor que `Backoffice:ApiKey` no appsettings). " +
                    "Obrigatória em todas as operações sob /api/backoffice/."
            };

            options.AddSecurityDefinition(BackofficeApiKeyOperationFilter.SecuritySchemeId, backofficeApiKey);

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                { new OpenApiSecuritySchemeReference("Bearer", document, null), new List<string>() }
            });

            options.OperationFilter<TenantHeaderOperationFilter>();
            options.OperationFilter<BackofficeApiKeyOperationFilter>();
        });

        services.AddTenancyModule(configuration);
        services.AddIdentityModule(configuration);
        services.AddBackofficeModule(configuration);
        services.AddMembershipModule();
        services.AddPaymentsModule(configuration);
        return services;
    }
}
