using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SocioTorcedor.Api.Middleware;
using SocioTorcedor.Api.Options;
using SocioTorcedor.Api.Tenancy;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;
using SocioTorcedor.Modules.Tenancy.Application.DTOs;

namespace SocioTorcedor.Api.Tests.Middleware;

public sealed class DynamicCorsMiddlewareTests
{
    private static TenantContext CreateTenant(params string[] origins) =>
        new(Guid.NewGuid(), "T", "t", "cs", origins);

    private static ITenantResolver UnusedResolver() => Substitute.For<ITenantResolver>();

    private static IOptions<BackofficeOptions> EmptyBackofficeOptions() =>
        Microsoft.Extensions.Options.Options.Create(new BackofficeOptions());

    private static IOptions<BackofficeOptions> BackofficeOptionsWith(params string[] origins) =>
        Microsoft.Extensions.Options.Options.Create(new BackofficeOptions { AllowedOrigins = [..origins] });

    [Fact]
    public async Task InvokeAsync_BypassSwaggerJson_CallsNext()
    {
        var nextCalled = false;
        var mw = new DynamicCorsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/swagger/v1/swagger.json";

        await mw.InvokeAsync(context, UnusedResolver(), EmptyBackofficeOptions());

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_BypassScalar_CallsNext()
    {
        var nextCalled = false;
        var mw = new DynamicCorsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/scalar";

        await mw.InvokeAsync(context, UnusedResolver(), EmptyBackofficeOptions());

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

        await mw.InvokeAsync(context, UnusedResolver(), EmptyBackofficeOptions());

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        context.Response.Headers["Access-Control-Allow-Origin"].ToString().Should()
            .Be("https://flamengo.example.com");
        context.Response.Headers["Access-Control-Allow-Credentials"].ToString().Should().Be("true");
        context.Response.Headers["Access-Control-Allow-Headers"].ToString().Should().Contain("X-Tenant-Id");
        context.Response.Headers["Access-Control-Allow-Headers"].ToString().Should().Contain("X-Api-Key");
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

        await mw.InvokeAsync(context, UnusedResolver(), EmptyBackofficeOptions());

        context.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        context.Response.Headers.ContainsKey("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_Options_ResolvesTenantFromHeader_WhenNotInItems()
    {
        var mw = new DynamicCorsMiddleware(_ => Task.CompletedTask);

        var tenant = CreateTenant("http://feira.localhost:5173");
        var resolver = Substitute.For<ITenantResolver>();
        resolver.ResolveAsync("feira", Arg.Any<CancellationToken>()).Returns(tenant);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/x";
        context.Request.Method = HttpMethods.Options;
        context.Request.Headers.Origin = "http://feira.localhost:5173";
        context.Request.Headers["X-Tenant-Id"] = "feira";

        await mw.InvokeAsync(context, resolver, EmptyBackofficeOptions());

        context.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        context.Response.Headers["Access-Control-Allow-Origin"].ToString().Should()
            .Be("http://feira.localhost:5173");
        context.Items[HttpContextTenantContext.TenantContextItemKey].Should().Be(tenant);
    }

    [Fact]
    public async Task InvokeAsync_Options_ResolvesTenantFromLocalhostOrigin_WhenHeaderMissing()
    {
        var mw = new DynamicCorsMiddleware(_ => Task.CompletedTask);

        var tenant = CreateTenant("http://feira.localhost:5173");
        var resolver = Substitute.For<ITenantResolver>();
        resolver.ResolveAsync("feira", Arg.Any<CancellationToken>()).Returns(tenant);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/x";
        context.Request.Method = HttpMethods.Options;
        context.Request.Headers.Origin = "http://feira.localhost:5173";

        await mw.InvokeAsync(context, resolver, EmptyBackofficeOptions());

        context.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        context.Response.Headers["Access-Control-Allow-Origin"].ToString().Should()
            .Be("http://feira.localhost:5173");
    }

    [Fact]
    public async Task InvokeAsync_Options_NoTenantSlug_NoCorsHeaders()
    {
        var mw = new DynamicCorsMiddleware(_ => Task.CompletedTask);
        var resolver = Substitute.For<ITenantResolver>();

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/x";
        context.Request.Method = HttpMethods.Options;
        context.Request.Headers.Origin = "https://evil.com";

        await mw.InvokeAsync(context, resolver, EmptyBackofficeOptions());

        context.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        context.Response.Headers.ContainsKey("Access-Control-Allow-Origin").Should().BeFalse();
        await resolver.DidNotReceive().ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
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

        await mw.InvokeAsync(context, UnusedResolver(), EmptyBackofficeOptions());

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

        await mw.InvokeAsync(context, UnusedResolver(), EmptyBackofficeOptions());

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

        await mw.InvokeAsync(context, UnusedResolver(), EmptyBackofficeOptions());

        nextCalled.Should().BeTrue();
        context.Response.Headers.ContainsKey("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_Get_ResolvesTenantFromHeader_WhenNotInItems()
    {
        var nextCalled = false;
        var mw = new DynamicCorsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var tenant = CreateTenant("http://feira.localhost:5173");
        var resolver = Substitute.For<ITenantResolver>();
        resolver.ResolveAsync("feira", Arg.Any<CancellationToken>()).Returns(tenant);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/x";
        context.Request.Method = HttpMethods.Get;
        context.Request.Headers.Origin = "http://feira.localhost:5173";
        context.Request.Headers["X-Tenant-Id"] = "feira";

        await mw.InvokeAsync(context, resolver, EmptyBackofficeOptions());

        nextCalled.Should().BeTrue();
        context.Response.Headers["Access-Control-Allow-Origin"].ToString().Should()
            .Be("http://feira.localhost:5173");
        context.Items[HttpContextTenantContext.TenantContextItemKey].Should().Be(tenant);
    }

    [Fact]
    public async Task InvokeAsync_BackofficeOptions_AllowedOrigin_SetsCorsHeadersAnd204()
    {
        var nextCalled = false;
        var mw = new DynamicCorsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/backoffice/plans";
        context.Request.Method = HttpMethods.Options;
        context.Request.Headers.Origin = "http://localhost:5173";

        await mw.InvokeAsync(context, UnusedResolver(), BackofficeOptionsWith("http://localhost:5173"));

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        context.Response.Headers["Access-Control-Allow-Origin"].ToString().Should()
            .Be("http://localhost:5173");
    }

    [Fact]
    public async Task InvokeAsync_BackofficeOptions_DisallowedOrigin_NoCorsHeaders()
    {
        var mw = new DynamicCorsMiddleware(_ => Task.CompletedTask);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/backoffice/tenants";
        context.Request.Method = HttpMethods.Options;
        context.Request.Headers.Origin = "https://evil.com";

        await mw.InvokeAsync(context, UnusedResolver(), BackofficeOptionsWith("http://localhost:5173"));

        context.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        context.Response.Headers.ContainsKey("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_BackofficeGet_AllowedOrigin_AddsCorsHeadersAndCallsNext()
    {
        var nextCalled = false;
        var mw = new DynamicCorsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/backoffice/plans";
        context.Request.Method = HttpMethods.Get;
        context.Request.Headers.Origin = "http://localhost:5173";

        await mw.InvokeAsync(context, UnusedResolver(), BackofficeOptionsWith("http://localhost:5173"));

        nextCalled.Should().BeTrue();
        context.Response.Headers["Access-Control-Allow-Origin"].ToString().Should()
            .Be("http://localhost:5173");
    }
}
