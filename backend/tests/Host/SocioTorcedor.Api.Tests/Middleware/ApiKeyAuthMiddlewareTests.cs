using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SocioTorcedor.Api.Middleware;
using SocioTorcedor.Api.Options;

namespace SocioTorcedor.Api.Tests.Middleware;

public sealed class ApiKeyAuthMiddlewareTests
{
    private static IOptions<BackofficeOptions> CreateOptions(string key) =>
        Microsoft.Extensions.Options.Options.Create(new BackofficeOptions { ApiKey = key });

    [Fact]
    public async Task NonBackofficePath_CallsNextWithoutCheckingKey()
    {
        var next = false;
        var mw = new ApiKeyAuthMiddleware(_ =>
        {
            next = true;
            return Task.CompletedTask;
        }, CreateOptions("secret"));

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/Auth/login";

        await mw.InvokeAsync(context);

        next.Should().BeTrue();
    }

    [Fact]
    public async Task BackofficePath_MissingKey_Returns401()
    {
        var next = false;
        var mw = new ApiKeyAuthMiddleware(_ =>
        {
            next = true;
            return Task.CompletedTask;
        }, CreateOptions("expected"));

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/backoffice/plans";
        context.Response.Body = new MemoryStream();

        await mw.InvokeAsync(context);

        next.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task BackofficePath_ValidKey_CallsNext()
    {
        var next = false;
        var mw = new ApiKeyAuthMiddleware(_ =>
        {
            next = true;
            return Task.CompletedTask;
        }, CreateOptions("expected"));

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/backoffice/plans";
        context.Request.Headers["X-Api-Key"] = "expected";

        await mw.InvokeAsync(context);

        next.Should().BeTrue();
    }

    [Fact]
    public async Task BackofficePath_EmptyConfiguredKey_Returns503()
    {
        var next = false;
        var mw = new ApiKeyAuthMiddleware(_ =>
        {
            next = true;
            return Task.CompletedTask;
        }, CreateOptions("  "));

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/backoffice/plans";
        context.Response.Body = new MemoryStream();

        await mw.InvokeAsync(context);

        next.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }
}
