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
        var legal = Substitute.For<ILegalDocumentRepository>();
        var handler = new RegisterUserHandler(identity, legal, new UnresolvedTenant());

        var r = await handler.Handle(
            new RegisterUserCommand(
                "a@b.com",
                "password1",
                "A",
                "B",
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                null),
            CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Tenant.Required");
        await identity.DidNotReceive()
            .RegisterAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());
        await legal.DidNotReceive()
            .ValidateRegistrationAcceptancesAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delegates_to_identity_service()
    {
        var tid = Guid.NewGuid();
        var termsId = Guid.NewGuid();
        var privacyId = Guid.NewGuid();
        var identity = Substitute.For<IIdentityService>();
        var legal = Substitute.For<ILegalDocumentRepository>();
        legal.ValidateRegistrationAcceptancesAsync(termsId, privacyId, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        identity.RegisterAsync(
                "a@b.com",
                "password1",
                "A",
                "B",
                tid,
                termsId,
                privacyId,
                "127.0.0.1",
                "ua-test",
                Arg.Any<CancellationToken>())
            .Returns(Result<AuthResultDto>.Ok(new AuthResultDto("tok", DateTime.UtcNow.AddHours(1))));

        var handler = new RegisterUserHandler(identity, legal, new ResolvedTenant(tid));

        var r = await handler.Handle(
            new RegisterUserCommand(
                "a@b.com",
                "password1",
                "A",
                "B",
                termsId,
                privacyId,
                "127.0.0.1",
                "ua-test"),
            CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        await identity.Received(1).RegisterAsync(
            "a@b.com",
            "password1",
            "A",
            "B",
            tid,
            termsId,
            privacyId,
            "127.0.0.1",
            "ua-test",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task When_legal_acceptance_invalid_does_not_call_identity()
    {
        var tid = Guid.NewGuid();
        var termsId = Guid.NewGuid();
        var privacyId = Guid.NewGuid();
        var identity = Substitute.For<IIdentityService>();
        var legal = Substitute.For<ILegalDocumentRepository>();
        legal.ValidateRegistrationAcceptancesAsync(termsId, privacyId, Arg.Any<CancellationToken>())
            .Returns(Result.Fail(Error.Validation("Identity.LegalAcceptanceInvalid", "bad")));

        var handler = new RegisterUserHandler(identity, legal, new ResolvedTenant(tid));

        var r = await handler.Handle(
            new RegisterUserCommand(
                "a@b.com",
                "password1",
                "A",
                "B",
                termsId,
                privacyId,
                null,
                null),
            CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Identity.LegalAcceptanceInvalid");
        await identity.DidNotReceive().RegisterAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
