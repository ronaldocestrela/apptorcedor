using FluentAssertions;
using SocioTorcedor.Api.Middleware;
using SocioTorcedor.Api.Tenancy;
using SocioTorcedor.Modules.Tenancy.Application.DTOs;

namespace SocioTorcedor.Api.Tests.Middleware;

public sealed class DynamicCorsMiddlewareTests
{
    private static TenantContext CreateTenant(params string[] origins) =>
        new(Guid.NewGuid(), "T", "t", "cs", origins);

    [Fact]
    public async Task InvokeAsync_BypassSwagger_CallsNext()
    {
        var nextCalled = false;
        var mw = new DynamicCorsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/swagger/index.html";

        await mw.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_Options_AllowedOrigin_SetsCorsHeadersAnd204()
    {
        var nextCalled = false;
        var mw = new DynamicCorsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/x";
        context.Request.Method = HttpMethods.Options;
        context.Request.Headers.Origin = "https://flamengo.example.com";
        context.Items[HttpContextTenantContext.TenantContextItemKey] =
            CreateTenant("https://flamengo.example.com");

        await mw.InvokeAsync(context);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        context.Response.Headers["Access-Control-Allow-Origin"].ToString().Should()
            .Be("https://flamengo.example.com");
        context.Response.Headers["Access-Control-Allow-Credentials"].ToString().Should().Be("true");
        context.Response.Headers["Access-Control-Allow-Headers"].ToString().Should().Contain("X-Tenant-Id");
    }

    [Fact]
    public async Task InvokeAsync_Options_DisallowedOrigin_NoCorsHeaders()
    {
        var mw = new DynamicCorsMiddleware(_ => Task.CompletedTask);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/x";
        context.Request.Method = HttpMethods.Options;
        context.Request.Headers.Origin = "https://evil.com";
        context.Items[HttpContextTenantContext.TenantContextItemKey] =
            CreateTenant("https://flamengo.example.com");

        await mw.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        context.Response.Headers.ContainsKey("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_Get_AllowedOrigin_AddsCorsHeaders()
    {
        var nextCalled = false;
        var mw = new DynamicCorsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/x";
        context.Request.Method = HttpMethods.Get;
        context.Request.Headers.Origin = "https://flamengo.example.com/";
        context.Items[HttpContextTenantContext.TenantContextItemKey] =
            CreateTenant("https://flamengo.example.com");

        await mw.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.Headers["Access-Control-Allow-Origin"].ToString().Should()
            .Be("https://flamengo.example.com/");
    }

    [Fact]
    public async Task InvokeAsync_Get_DisallowedOrigin_DoesNotAddCorsHeaders()
    {
        var nextCalled = false;
        var mw = new DynamicCorsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/x";
        context.Request.Method = HttpMethods.Get;
        context.Request.Headers.Origin = "https://evil.com";
        context.Items[HttpContextTenantContext.TenantContextItemKey] =
            CreateTenant("https://flamengo.example.com");

        await mw.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.Headers.ContainsKey("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_NoOrigin_CallsNextWithoutHeaders()
    {
        var nextCalled = false;
        var mw = new DynamicCorsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/x";
        context.Items[HttpContextTenantContext.TenantContextItemKey] =
            CreateTenant("https://flamengo.example.com");

        await mw.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.Headers.ContainsKey("Access-Control-Allow-Origin").Should().BeFalse();
    }
}
