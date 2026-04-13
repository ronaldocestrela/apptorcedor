using System.Security.Claims;
using AppTorcedor.Infrastructure.Auditing;
using Microsoft.Extensions.Primitives;

namespace AppTorcedor.Api.Middleware;

public sealed class RequestContextMiddleware(RequestDelegate next, ILogger<RequestContextMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ICurrentAuditContext auditContext)
    {
        var correlationId = context.Request.Headers.TryGetValue("X-Correlation-Id", out StringValues headerValue) && !StringValues.IsNullOrEmpty(headerValue)
            ? headerValue.ToString()
            : Guid.NewGuid().ToString("N");

        context.Response.Headers["X-Correlation-Id"] = correlationId;
        auditContext.CorrelationId = correlationId;

        var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        auditContext.UserId = Guid.TryParse(sub, out var uid) ? uid : null;

        using (logger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = correlationId, ["UserId"] = sub ?? "anonymous" }))
        {
            await next(context).ConfigureAwait(false);
        }
    }
}
