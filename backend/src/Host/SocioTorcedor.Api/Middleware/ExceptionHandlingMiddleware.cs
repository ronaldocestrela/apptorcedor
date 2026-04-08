using System.Net;
using System.Text.Json;
using SocioTorcedor.BuildingBlocks.Domain.Abstractions;

namespace SocioTorcedor.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (BusinessRuleValidationException ex)
        {
            logger.LogWarning(ex, "Business rule validation failed.");
            await WriteProblemAsync(
                context,
                HttpStatusCode.BadRequest,
                "Business rule violation",
                ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception.");
            await WriteProblemAsync(
                context,
                HttpStatusCode.InternalServerError,
                "Server error",
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        HttpStatusCode status,
        string title,
        string detail)
    {
        if (context.Response.HasStarted)
            throw new InvalidOperationException("The response has already started.");

        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/problem+json";

        var body = new
        {
            type = "about:blank",
            title,
            detail,
            status = (int)status
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }
}
