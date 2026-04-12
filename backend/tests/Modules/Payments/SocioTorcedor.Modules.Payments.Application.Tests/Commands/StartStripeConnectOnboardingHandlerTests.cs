using FluentAssertions;
using NSubstitute;
using SocioTorcedor.BuildingBlocks.Application.Payments;
using SocioTorcedor.Modules.Payments.Application.Commands.StartStripeConnectOnboarding;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Domain.Entities;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Application.Tests.Commands;

public sealed class StartStripeConnectOnboardingHandlerTests
{
    private static StartStripeConnectOnboardingHandler CreateHandler(
        ITenantMasterPaymentsRepository repo,
        IPaymentsGatewayMetadata? gateway = null,
        IPaymentProvider? provider = null)
    {
        var g = gateway ?? Substitute.For<IPaymentsGatewayMetadata>();
        g.IsStripeEnabled.Returns(true);
        var p = provider ?? Substitute.For<IPaymentProvider>();
        return new StartStripeConnectOnboardingHandler(repo, g, p);
    }

    [Fact]
    public async Task When_stripe_disabled_returns_Payments_Stripe_Disabled()
    {
        var tenantId = Guid.NewGuid();
        var repo = Substitute.For<ITenantMasterPaymentsRepository>();
        var gateway = Substitute.For<IPaymentsGatewayMetadata>();
        gateway.IsStripeEnabled.Returns(false);
        var provider = Substitute.For<IPaymentProvider>();
        var handler = new StartStripeConnectOnboardingHandler(repo, gateway, provider);

        var r = await handler.Handle(
            new StartStripeConnectOnboardingCommand(tenantId, "https://r", "https://ret"),
            CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Payments.Stripe.Disabled");
        await provider.DidNotReceive()
            .CreateConnectExpressAccountAsync(Arg.Any<CreateConnectExpressAccountRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task When_no_existing_connect_creates_express_account_persists_and_returns_link()
    {
        var tenantId = Guid.NewGuid();
        var repo = Substitute.For<ITenantMasterPaymentsRepository>();
        repo.GetStripeConnectByTenantIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns((TenantStripeConnectAccount?)null);

        var provider = Substitute.For<IPaymentProvider>();
        provider.CreateConnectExpressAccountAsync(
                Arg.Is<CreateConnectExpressAccountRequest>(req =>
                    req.Country == "BR" &&
                    req.Metadata["tenant_id"] == tenantId.ToString("D") &&
                    req.IdempotencyKey == $"connect-acct:{tenantId:N}"),
                Arg.Any<CancellationToken>())
            .Returns(new CreateConnectExpressAccountResult("acct_new"));

        provider.CreateConnectAccountLinkAsync(
                Arg.Is<CreateConnectAccountLinkRequest>(req =>
                    req.AccountId == "acct_new" &&
                    req.RefreshUrl == "https://refresh" &&
                    req.ReturnUrl == "https://return"),
                Arg.Any<CancellationToken>())
            .Returns(new CreateConnectAccountLinkResult("https://connect.stripe.com/setup"));

        var handler = CreateHandler(repo, provider: provider);

        var r = await handler.Handle(
            new StartStripeConnectOnboardingCommand(tenantId, "https://refresh", "https://return"),
            CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value!.Url.Should().Be("https://connect.stripe.com/setup");
        await repo.Received(1).AddStripeConnectAsync(
            Arg.Is<TenantStripeConnectAccount>(a => a.TenantId == tenantId && a.StripeAccountId == "acct_new"),
            Arg.Any<CancellationToken>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task When_connect_already_exists_skips_account_creation_and_uses_existing_account_id()
    {
        var tenantId = Guid.NewGuid();
        var existing = TenantStripeConnectAccount.Create(tenantId, "acct_existing");

        var repo = Substitute.For<ITenantMasterPaymentsRepository>();
        repo.GetStripeConnectByTenantIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns(existing);

        var provider = Substitute.For<IPaymentProvider>();
        provider.CreateConnectAccountLinkAsync(
                Arg.Is<CreateConnectAccountLinkRequest>(req =>
                    req.AccountId == "acct_existing" &&
                    req.RefreshUrl == "https://r2" &&
                    req.ReturnUrl == "https://ret2"),
                Arg.Any<CancellationToken>())
            .Returns(new CreateConnectAccountLinkResult("https://link-2"));

        var handler = CreateHandler(repo, provider: provider);

        var r = await handler.Handle(
            new StartStripeConnectOnboardingCommand(tenantId, "https://r2", "https://ret2"),
            CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value!.Url.Should().Be("https://link-2");
        await provider.DidNotReceive()
            .CreateConnectExpressAccountAsync(Arg.Any<CreateConnectExpressAccountRequest>(), Arg.Any<CancellationToken>());
        await repo.DidNotReceive().AddStripeConnectAsync(Arg.Any<TenantStripeConnectAccount>(), Arg.Any<CancellationToken>());
        await repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
