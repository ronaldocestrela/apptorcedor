using FluentAssertions;
using NSubstitute;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Identity.Application.Commands.RegisterUser;
using SocioTorcedor.Modules.Identity.Application.Contracts;
using SocioTorcedor.Modules.Identity.Application.DTOs;

namespace SocioTorcedor.Modules.Identity.Application.Tests.Commands;

public class RegisterUserHandlerTests
{
    private sealed class ResolvedTenant(Guid tenantId) : ICurrentTenantContext
    {
        public bool IsResolved => true;

        public Guid TenantId { get; } = tenantId;

        public string TenantConnectionString => "test";
    }

    private sealed class UnresolvedTenant : ICurrentTenantContext
    {
        public bool IsResolved => false;

        public Guid TenantId => throw new InvalidOperationException();

        public string TenantConnectionString => throw new InvalidOperationException();
    }

    [Fact]
    public async Task When_tenant_unresolved_returns_failure()
    {
        var identity = Substitute.For<IIdentityService>();
        var handler = new RegisterUserHandler(identity, new UnresolvedTenant());

        var r = await handler.Handle(
            new RegisterUserCommand("a@b.com", "password1", "A", "B"),
            CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Tenant.Required");
        await identity.DidNotReceive()
            .RegisterAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delegates_to_identity_service()
    {
        var tid = Guid.NewGuid();
        var identity = Substitute.For<IIdentityService>();
        identity.RegisterAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), tid, Arg.Any<CancellationToken>())
            .Returns(Result<AuthResultDto>.Ok(new AuthResultDto("tok", DateTime.UtcNow.AddHours(1))));

        var handler = new RegisterUserHandler(identity, new ResolvedTenant(tid));

        var r = await handler.Handle(
            new RegisterUserCommand("a@b.com", "password1", "A", "B"),
            CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        await identity.Received(1).RegisterAsync("a@b.com", "password1", "A", "B", tid, Arg.Any<CancellationToken>());
    }
}
