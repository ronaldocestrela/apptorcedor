using SocioTorcedor.Api.Middleware;

namespace SocioTorcedor.Api.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseSocioTorcedorMiddleware(this IApplicationBuilder app) =>
        app
            .UseWhen(
                ctx => ctx.Request.Path.StartsWithSegments("/api/webhooks"),
                branch => branch.Use(async (ctx, next) =>
                {
                    ctx.Request.EnableBuffering();
                    await next();
                }))
            .UseMiddleware<ExceptionHandlingMiddleware>()
            .UseMiddleware<DynamicCorsMiddleware>()
            .UseMiddleware<TenantResolutionMiddleware>()
            .UseMiddleware<ApiKeyAuthMiddleware>();
}
