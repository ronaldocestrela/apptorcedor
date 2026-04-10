using System.Diagnostics.CodeAnalysis;
using SocioTorcedor.Api.Tenancy;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;
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

    public async Task InvokeAsync(HttpContext context, ITenantResolver tenantResolver)
    {
        if (ShouldBypass(context.Request.Path))
        {
            await next(context);
            return;
        }

        var tenant = await EnsureTenantContextAsync(context, tenantResolver);

        var origin = context.Request.Headers.Origin.ToString();
        var hasOrigin = !string.IsNullOrEmpty(origin);

        if (HttpMethods.IsOptions(context.Request.Method))
        {
            if (hasOrigin && tenant is not null)
            {
                var normalizedOrigin = origin.TrimEnd('/');
                var allowed = tenant.AllowedOrigins.Any(o =>
                    string.Equals(o.TrimEnd('/'), normalizedOrigin, StringComparison.OrdinalIgnoreCase));
                if (allowed)
                    WriteCorsHeaders(context.Response, origin);
            }

            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        if (tenant is not null && hasOrigin)
        {
            var normalizedOrigin = origin.TrimEnd('/');
            var allowed = tenant.AllowedOrigins.Any(o =>
                string.Equals(o.TrimEnd('/'), normalizedOrigin, StringComparison.OrdinalIgnoreCase));
            if (allowed)
                WriteCorsHeaders(context.Response, origin);
        }

        await next(context);
    }

    private static async Task<TenantContext?> EnsureTenantContextAsync(
        HttpContext context,
        ITenantResolver tenantResolver)
    {
        if (context.Items.TryGetValue(HttpContextTenantContext.TenantContextItemKey, out var raw) &&
            raw is TenantContext existing)
            return existing;

        var slug = context.Request.Headers["X-Tenant-Id"].ToString().Trim();
        if (string.IsNullOrEmpty(slug) && HttpMethods.IsOptions(context.Request.Method))
        {
            var origin = context.Request.Headers.Origin.ToString();
            if (TryGetSlugFromLocalhostOrigin(origin, out var fromOrigin))
                slug = fromOrigin;
        }

        if (string.IsNullOrEmpty(slug))
            return null;

        var tenant = await tenantResolver.ResolveAsync(slug, context.RequestAborted);
        if (tenant is not null)
            context.Items[HttpContextTenantContext.TenantContextItemKey] = tenant;

        return tenant;
    }

    /// <summary>
    /// Preflight CORS não envia <c>X-Tenant-Id</c>; em dev, extrai o slug do host <c>{slug}.localhost</c>.
    /// </summary>
    private static bool TryGetSlugFromLocalhostOrigin(
        string? origin,
        [NotNullWhen(true)] out string? slug)
    {
        slug = null;
        if (string.IsNullOrWhiteSpace(origin))
            return false;

        if (!Uri.TryCreate(origin.Trim(), UriKind.Absolute, out var uri))
            return false;

        var host = uri.IdnHost.Length > 0 ? uri.IdnHost : uri.Host;
        const string localhostSuffix = ".localhost";
        var idx = host.IndexOf(localhostSuffix, StringComparison.OrdinalIgnoreCase);
        if (idx <= 0)
            return false;

        var sub = host[..idx];
        if (string.IsNullOrEmpty(sub) || sub.Contains('.'))
            return false;

        slug = sub.ToLowerInvariant();
        return true;
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
