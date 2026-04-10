using SocioTorcedor.Api.Middleware;

namespace SocioTorcedor.Api.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseSocioTorcedorMiddleware(this IApplicationBuilder app) =>
        app
            .UseMiddleware<ExceptionHandlingMiddleware>()
            .UseMiddleware<DynamicCorsMiddleware>()
            .UseMiddleware<TenantResolutionMiddleware>()
            .UseMiddleware<ApiKeyAuthMiddleware>();
}
