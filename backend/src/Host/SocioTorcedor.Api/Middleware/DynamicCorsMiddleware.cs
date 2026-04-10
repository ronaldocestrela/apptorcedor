using SocioTorcedor.Api.Tenancy;
using SocioTorcedor.Modules.Tenancy.Application.DTOs;

namespace SocioTorcedor.Api.Middleware;

public sealed class DynamicCorsMiddleware(RequestDelegate next)
{
    private static readonly PathString[] BypassPrefixes =
    [
        "/health",
        "/swagger",
        "/scalar",
        "/api/backoffice"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldBypass(context.Request.Path))
        {
            await next(context);
            return;
        }

        if (context.Items.TryGetValue(HttpContextTenantContext.TenantContextItemKey, out var raw) &&
            raw is TenantContext tenant)
        {
            var origin = context.Request.Headers.Origin.ToString();
            if (string.IsNullOrEmpty(origin))
            {
                await next(context);
                return;
            }

            var normalizedOrigin = origin.TrimEnd('/');
            var allowed = tenant.AllowedOrigins.Any(o =>
                string.Equals(o.TrimEnd('/'), normalizedOrigin, StringComparison.OrdinalIgnoreCase));

            if (HttpMethods.IsOptions(context.Request.Method))
            {
                if (allowed)
                    WriteCorsHeaders(context.Response, origin);

                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return;
            }

            if (!allowed)
            {
                await next(context);
                return;
            }

            WriteCorsHeaders(context.Response, origin);
        }

        await next(context);
    }

    private static void WriteCorsHeaders(HttpResponse response, string origin)
    {
        response.Headers.Append("Access-Control-Allow-Origin", origin);
        response.Headers.Append("Access-Control-Allow-Credentials", "true");
        response.Headers.Append("Access-Control-Allow-Methods", "GET,POST,PUT,PATCH,DELETE,OPTIONS");
        response.Headers.Append("Access-Control-Allow-Headers", "Authorization,Content-Type,X-Tenant-Id,X-Api-Key");
        response.Headers.Append("Vary", "Origin");
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
