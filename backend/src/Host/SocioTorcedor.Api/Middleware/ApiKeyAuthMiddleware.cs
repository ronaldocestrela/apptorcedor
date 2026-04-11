using Microsoft.Extensions.Options;
using SocioTorcedor.Api.Options;

namespace SocioTorcedor.Api.Middleware;

public sealed class ApiKeyAuthMiddleware(RequestDelegate next, IOptions<BackofficeOptions> options)
{
    private readonly BackofficeOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/webhooks"))
        {
            await next(context);
            return;
        }

        if (!context.Request.Path.StartsWithSegments("/api/backoffice"))
        {
            await next(context);
            return;
        }

        if (HttpMethods.IsOptions(context.Request.Method))
        {
            await next(context);
            return;
        }

        var configuredKey = _options.ApiKey?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(configuredKey))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new { error = "Backoffice API key is not configured." });
            return;
        }

        var provided = context.Request.Headers["X-Api-Key"].ToString().Trim();
        if (!string.Equals(provided, configuredKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or missing API key." });
            return;
        }

        await next(context);
    }
}
