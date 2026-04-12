using FluentAssertions;
using NSubstitute;
using SocioTorcedor.BuildingBlocks.Application.Payments;
using SocioTorcedor.Modules.Payments.Application.Commands.SyncStripeConnectStatus;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Domain.Entities;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Application.Tests.Commands;

public sealed class SyncStripeConnectStatusHandlerTests
{
    [Fact]
    public async Task When_stripe_disabled_returns_Payments_Stripe_Disabled()
    {
        var tenantId = Guid.NewGuid();
        var repo = Substitute.For<ITenantMasterPaymentsRepository>();
        var gateway = Substitute.For<IPaymentsGatewayMetadata>();
        gateway.IsStripeEnabled.Returns(false);
        var provider = Substitute.For<IPaymentProvider>();
        var handler = new SyncStripeConnectStatusHandler(repo, gateway, provider);

        var r = await handler.Handle(new SyncStripeConnectStatusCommand(tenantId), CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Payments.Stripe.Disabled");
        await provider.DidNotReceive()
            .GetConnectAccountStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task When_no_connect_row_returns_Connect_NotFound()
    {
        var tenantId = Guid.NewGuid();
        var repo = Substitute.For<ITenantMasterPaymentsRepository>();
        repo.GetStripeConnectByTenantIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns((TenantStripeConnectAccount?)null);
        var gateway = Substitute.For<IPaymentsGatewayMetadata>();
        gateway.IsStripeEnabled.Returns(true);
        var provider = Substitute.For<IPaymentProvider>();
        var handler = new SyncStripeConnectStatusHandler(repo, gateway, provider);

        var r = await handler.Handle(new SyncStripeConnectStatusCommand(tenantId), CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Payments.Connect.NotFound");
        await provider.DidNotReceive()
            .GetConnectAccountStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task When_row_exists_fetches_stripe_syncs_and_saves()
    {
        var tenantId = Guid.NewGuid();
        var row = TenantStripeConnectAccount.Create(tenantId, "acct_sync");

        var repo = Substitute.For<ITenantMasterPaymentsRepository>();
        repo.GetStripeConnectByTenantIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns(row);

        var gateway = Substitute.For<IPaymentsGatewayMetadata>();
        gateway.IsStripeEnabled.Returns(true);

        var provider = Substitute.For<IPaymentProvider>();
        provider.GetConnectAccountStatusAsync("acct_sync", Arg.Any<CancellationToken>())
            .Returns(new ConnectAccountStatusResult("acct_sync", true, true, true));

        var handler = new SyncStripeConnectStatusHandler(repo, gateway, provider);

        var r = await handler.Handle(new SyncStripeConnectStatusCommand(tenantId), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value!.IsConfigured.Should().BeTrue();
        r.Value.StripeAccountId.Should().Be("acct_sync");
        r.Value.OnboardingStatus.Should().Be((int)StripeConnectAccountStatus.Enabled);
        r.Value.ChargesEnabled.Should().BeTrue();
        r.Value.PayoutsEnabled.Should().BeTrue();
        r.Value.DetailsSubmitted.Should().BeTrue();

        await provider.Received(1).GetConnectAccountStatusAsync("acct_sync", Arg.Any<CancellationToken>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
