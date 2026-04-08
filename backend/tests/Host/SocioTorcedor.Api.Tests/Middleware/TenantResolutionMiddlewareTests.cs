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
    public async Task InvokeAsync_NoSubdomain_Returns404()
    {
        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/x";
        context.Request.Headers.Host = "localhost";
        context.Response.Body = new MemoryStream();
        var resolver = Substitute.For<ITenantResolver>();

        await middleware.InvokeAsync(context, resolver);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
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
        context.Request.Headers.Host = "missing.example.com";
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
        context.Request.Headers.Host = "flamengo.example.com";
        var resolver = Substitute.For<ITenantResolver>();
        resolver.ResolveAsync("flamengo", Arg.Any<CancellationToken>()).Returns(tenant);

        await middleware.InvokeAsync(context, resolver);

        nextCalled.Should().BeTrue();
        context.Items[HttpContextTenantContext.TenantContextItemKey].Should().Be(tenant);
    }
}
