using FluentAssertions;
using NSubstitute;
using SocioTorcedor.Api.Middleware;
using SocioTorcedor.Api.Tenancy;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;
using SocioTorcedor.Modules.Tenancy.Application.DTOs;

namespace SocioTorcedor.Api.Tests.Middleware;

public sealed class TenantResolutionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_BypassBackoffice_CallsNextWithoutResolving()
    {
        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/backoffice/tenants";
        var resolver = Substitute.For<ITenantResolver>();

        await middleware.InvokeAsync(context, resolver);

        nextCalled.Should().BeTrue();
        context.Items.Should().NotContainKey(HttpContextTenantContext.TenantContextItemKey);
        await resolver.DidNotReceive().ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_BypassHealth_CallsNextWithoutResolving()
    {
        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/health";
        var resolver = Substitute.For<ITenantResolver>();

        await middleware.InvokeAsync(context, resolver);

        nextCalled.Should().BeTrue();
        context.Items.Should().NotContainKey(HttpContextTenantContext.TenantContextItemKey);
        await resolver.DidNotReceive().ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_BypassScalar_CallsNextWithoutResolving()
    {
        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/scalar";
        var resolver = Substitute.For<ITenantResolver>();

        await middleware.InvokeAsync(context, resolver);

        nextCalled.Should().BeTrue();
        context.Items.Should().NotContainKey(HttpContextTenantContext.TenantContextItemKey);
        await resolver.DidNotReceive().ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_NoTenantHeader_Returns400()
    {
        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/x";
        context.Response.Body = new MemoryStream();
        var resolver = Substitute.For<ITenantResolver>();

        await middleware.InvokeAsync(context, resolver);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        await resolver.DidNotReceive().ResolveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_TenantNotFound_Returns404()
    {
        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/x";
        context.Request.Headers["X-Tenant-Id"] = "missing";
        context.Response.Body = new MemoryStream();
        var resolver = Substitute.For<ITenantResolver>();
        resolver.ResolveAsync("missing", Arg.Any<CancellationToken>()).Returns((TenantContext?)null);

        await middleware.InvokeAsync(context, resolver);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task InvokeAsync_TenantFound_StoresContextAndCallsNext()
    {
        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var tenant = new TenantContext(
            Guid.NewGuid(),
            "Flamengo",
            "flamengo",
            "Server=.;Database=t1;",
            new[] { "https://flamengo.example.com" });

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/x";
        context.Request.Headers["X-Tenant-Id"] = "flamengo";
        var resolver = Substitute.For<ITenantResolver>();
        resolver.ResolveAsync("flamengo", Arg.Any<CancellationToken>()).Returns(tenant);

        await middleware.InvokeAsync(context, resolver);

        nextCalled.Should().BeTrue();
        context.Items[HttpContextTenantContext.TenantContextItemKey].Should().Be(tenant);
    }
}
