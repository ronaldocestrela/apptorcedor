using FluentAssertions;
using NSubstitute;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.Queries.GetStripeConnectStatus;
using SocioTorcedor.Modules.Payments.Domain.Entities;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Application.Tests.Queries;

public sealed class GetStripeConnectStatusHandlerTests
{
    [Fact]
    public async Task When_no_row_returns_not_configured_dto()
    {
        var tenantId = Guid.NewGuid();
        var repo = Substitute.For<ITenantMasterPaymentsRepository>();
        repo.GetStripeConnectByTenantIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns((TenantStripeConnectAccount?)null);

        var handler = new GetStripeConnectStatusHandler(repo);

        var r = await handler.Handle(new GetStripeConnectStatusQuery(tenantId), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value!.IsConfigured.Should().BeFalse();
        r.Value.StripeAccountId.Should().BeNull();
        r.Value.OnboardingStatus.Should().Be(0);
        r.Value.ChargesEnabled.Should().BeFalse();
        r.Value.PayoutsEnabled.Should().BeFalse();
        r.Value.DetailsSubmitted.Should().BeFalse();
    }

    [Fact]
    public async Task When_row_exists_returns_mapped_dto()
    {
        var tenantId = Guid.NewGuid();
        var row = TenantStripeConnectAccount.Create(tenantId, "acct_123");
        row.SyncFromStripe(chargesEnabled: true, payoutsEnabled: true, detailsSubmitted: true);

        var repo = Substitute.For<ITenantMasterPaymentsRepository>();
        repo.GetStripeConnectByTenantIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns(row);

        var handler = new GetStripeConnectStatusHandler(repo);

        var r = await handler.Handle(new GetStripeConnectStatusQuery(tenantId), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value!.IsConfigured.Should().BeTrue();
        r.Value.StripeAccountId.Should().Be("acct_123");
        r.Value.OnboardingStatus.Should().Be((int)StripeConnectAccountStatus.Enabled);
        r.Value.ChargesEnabled.Should().BeTrue();
        r.Value.PayoutsEnabled.Should().BeTrue();
        r.Value.DetailsSubmitted.Should().BeTrue();
    }
}
