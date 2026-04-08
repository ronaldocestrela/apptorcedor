using Microsoft.OpenApi.Models;
using SocioTorcedor.BuildingBlocks.Application;
using SocioTorcedor.Modules.Identity.Api;
using SocioTorcedor.Modules.Identity.Api.Controllers;
using SocioTorcedor.Modules.Identity.Application.Commands.LoginUser;
using SocioTorcedor.Modules.Identity.Application.Commands.RegisterUser;
using SocioTorcedor.Modules.Tenancy.Api;
using SocioTorcedor.Modules.Tenancy.Application.Queries.GetTenantBySubdomain;

namespace SocioTorcedor.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSocioTorcedorApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(AuthController).Assembly);

        services.AddBuildingBlocksApplication(
            typeof(GetTenantBySubdomainHandler).Assembly,
            typeof(RegisterUserHandler).Assembly,
            typeof(LoginUserHandler).Assembly);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Socio Torcedor API",
                Version = "v1"
            });

            var bearer = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme."
            };

            options.AddSecurityDefinition("Bearer", bearer);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { bearer, Array.Empty<string>() }
            });
        });

        services.AddTenancyModule(configuration);
        services.AddIdentityModule(configuration);
        return services;
    }
}
