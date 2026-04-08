using SocioTorcedor.Api.Tenancy;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;
using SocioTorcedor.Modules.Tenancy.Infrastructure.Services;

namespace SocioTorcedor.Api.Middleware;

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    private static readonly PathString[] BypassPrefixes =
    [
        "/health",
        "/swagger"
    ];

    public async Task InvokeAsync(HttpContext context, ITenantResolver tenantResolver)
    {
        if (ShouldBypass(context.Request.Path))
        {
            await next(context);
            return;
        }

        var host = context.Request.Headers.Host.ToString();
        var subdomain = SubdomainParser.TryExtractSubdomain(host);
        if (string.IsNullOrEmpty(subdomain))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant subdomain could not be determined from host." });
            return;
        }

        var tenant = await tenantResolver.ResolveAsync(subdomain, context.RequestAborted);
        if (tenant is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant not found." });
            return;
        }

        context.Items[HttpContextTenantContext.TenantContextItemKey] = tenant;
        await next(context);
    }

    private static bool ShouldBypass(PathString path)
    {
        foreach (var prefix in BypassPrefixes)
        {
            if (path.StartsWithSegments(prefix))
                return true;
        }

        return false;
    }
}
