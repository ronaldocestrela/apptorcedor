using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Identity.Application.Contracts;
using SocioTorcedor.Modules.Identity.Infrastructure.Entities;
using SocioTorcedor.Modules.Identity.Infrastructure.Options;
using SocioTorcedor.Modules.Identity.Infrastructure.Persistence;
using SocioTorcedor.Modules.Identity.Infrastructure.Services;

namespace SocioTorcedor.Modules.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddDbContext<TenantIdentityDbContext>((sp, builder) =>
        {
            var tenant = sp.GetRequiredService<ICurrentTenantContext>();
            if (!tenant.IsResolved)
                throw new InvalidOperationException("Tenant must be resolved before accessing the tenant Identity database.");

            builder.UseSqlServer(tenant.TenantConnectionString);
        });

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<TenantIdentityDbContext>()
            .AddDefaultTokenProviders();

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{JwtOptions.SectionName}' is missing.");

        if (string.IsNullOrWhiteSpace(jwt.SigningKey))
            throw new InvalidOperationException("Jwt:SigningKey must be configured.");

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        return services;
    }
}
