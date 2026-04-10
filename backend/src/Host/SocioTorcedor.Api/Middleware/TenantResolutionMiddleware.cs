using SocioTorcedor.Api.Tenancy;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;
using SocioTorcedor.Modules.Tenancy.Application.DTOs;

namespace SocioTorcedor.Api.Middleware;

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    private static readonly PathString[] BypassPrefixes =
    [
        "/health",
        "/swagger",
        "/scalar",
        "/api/backoffice"
    ];

    public async Task InvokeAsync(HttpContext context, ITenantResolver tenantResolver)
    {
        if (ShouldBypass(context.Request.Path))
        {
            await next(context);
            return;
        }

        if (context.Items.TryGetValue(HttpContextTenantContext.TenantContextItemKey, out var existing) &&
            existing is TenantContext)
        {
            await next(context);
            return;
        }

        var slug = context.Request.Headers["X-Tenant-Id"].ToString().Trim();
        if (string.IsNullOrEmpty(slug))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "Header 'X-Tenant-Id' is required." });
            return;
        }

        var tenant = await tenantResolver.ResolveAsync(slug, context.RequestAborted);
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
