using FluentAssertions;
using NSubstitute;
using SocioTorcedor.BuildingBlocks.Application.Payments;
using SocioTorcedor.Modules.Backoffice.Domain.Enums;
using SocioTorcedor.Modules.Payments.Application.Commands.AttachTenantSaasPaymentMethod;
using SocioTorcedor.Modules.Payments.Application.Commands.CreateTenantSaasSetupIntent;
using SocioTorcedor.Modules.Payments.Application.Commands.DetachTenantSaasPaymentMethod;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.Queries.ListTenantSaasPaymentMethods;
using SocioTorcedor.Modules.Payments.Domain.Entities;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Application.Tests.Commands;

public sealed class TenantSaasPaymentMethodsHandlersTests
{
    private static TenantBillingSubscription ActiveSub(Guid tenantId, string customerId, string subId = "sub_test") =>
        TenantBillingSubscription.Start(
            tenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            BillingCycle.Monthly,
            10m,
            "BRL",
            customerId,
            subId,
            BillingSubscriptionStatus.Active,
            null);

    [Fact]
    public async Task List_cards_fails_when_no_subscription()
    {
        var tenantId = Guid.NewGuid();
        var repo = Substitute.For<ITenantMasterPaymentsRepository>();
        repo.GetActiveSubscriptionByTenantAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns((TenantBillingSubscription?)null);

        var provider = Substitute.For<IPaymentProvider>();
        var handler = new ListTenantSaasPaymentMethodsHandler(repo, provider);

        var r = await handler.Handle(new ListTenantSaasPaymentMethodsQuery(tenantId), CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Contain("NotFound");
        await provider.DidNotReceive().ListSaasCustomerPaymentMethodsAsync(Arg.Any<ListSaasCustomerPaymentMethodsRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_cards_returns_items_from_provider()
    {
        var tenantId = Guid.NewGuid();
        var sub = ActiveSub(tenantId, "cus_1");
        var repo = Substitute.For<ITenantMasterPaymentsRepository>();
        repo.GetActiveSubscriptionByTenantAsync(tenantId, Arg.Any<CancellationToken>()).Returns(sub);

        var provider = Substitute.For<IPaymentProvider>();
        provider.ListSaasCustomerPaymentMethodsAsync(
                Arg.Any<ListSaasCustomerPaymentMethodsRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new ListSaasCustomerPaymentMethodsResult(new[]
            {
                new SaasPaymentMethodListItem("pm_1", "visa", "4242", 12, 2030, true)
            }));

        var handler = new ListTenantSaasPaymentMethodsHandler(repo, provider);
        var r = await handler.Handle(new ListTenantSaasPaymentMethodsQuery(tenantId), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value!.Should().ContainSingle(x => x.Id == "pm_1" && x.IsDefault);
    }

    [Fact]
    public async Task Setup_intent_fails_when_no_customer()
    {
        var tenantId = Guid.NewGuid();
        var repo = Substitute.For<ITenantMasterPaymentsRepository>();
        repo.GetActiveSubscriptionByTenantAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns((TenantBillingSubscription?)null);

        var provider = Substitute.For<IPaymentProvider>();
        var handler = new CreateTenantSaasSetupIntentHandler(repo, provider);

        var r = await handler.Handle(new CreateTenantSaasSetupIntentCommand(tenantId), CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        await provider.DidNotReceive().CreateSaasSetupIntentAsync(Arg.Any<CreateSaasSetupIntentRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Attach_fails_when_payment_method_id_empty()
    {
        var tenantId = Guid.NewGuid();
        var repo = Substitute.For<ITenantMasterPaymentsRepository>();
        var provider = Substitute.For<IPaymentProvider>();
        var handler = new AttachTenantSaasPaymentMethodHandler(repo, provider);

        var r = await handler.Handle(
            new AttachTenantSaasPaymentMethodCommand(tenantId, "  ", true),
            CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Payments.PaymentMethod.Required");
    }

    [Fact]
    public async Task Detach_fails_when_only_one_card()
    {
        var tenantId = Guid.NewGuid();
        var sub = ActiveSub(tenantId, "cus_1");
        var repo = Substitute.For<ITenantMasterPaymentsRepository>();
        repo.GetActiveSubscriptionByTenantAsync(tenantId, Arg.Any<CancellationToken>()).Returns(sub);

        var provider = Substitute.For<IPaymentProvider>();
        provider.ListSaasCustomerPaymentMethodsAsync(
                Arg.Any<ListSaasCustomerPaymentMethodsRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new ListSaasCustomerPaymentMethodsResult(new[]
            {
                new SaasPaymentMethodListItem("pm_only", "visa", "4242", 12, 2030, true)
            }));

        var handler = new DetachTenantSaasPaymentMethodHandler(repo, provider);
        var r = await handler.Handle(
            new DetachTenantSaasPaymentMethodCommand(tenantId, "pm_only"),
            CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Contain("LastCard");
        await provider.DidNotReceive().DetachSaasPaymentMethodAsync(Arg.Any<DetachSaasPaymentMethodRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Detach_succeeds_when_two_cards()
    {
        var tenantId = Guid.NewGuid();
        var sub = ActiveSub(tenantId, "cus_1");
        var repo = Substitute.For<ITenantMasterPaymentsRepository>();
        repo.GetActiveSubscriptionByTenantAsync(tenantId, Arg.Any<CancellationToken>()).Returns(sub);

        var provider = Substitute.For<IPaymentProvider>();
        provider.ListSaasCustomerPaymentMethodsAsync(
                Arg.Any<ListSaasCustomerPaymentMethodsRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new ListSaasCustomerPaymentMethodsResult(new[]
            {
                new SaasPaymentMethodListItem("pm_a", "visa", "4242", 12, 2030, true),
                new SaasPaymentMethodListItem("pm_b", "mastercard", "5555", 6, 2029, false)
            }));

        var handler = new DetachTenantSaasPaymentMethodHandler(repo, provider);
        var r = await handler.Handle(
            new DetachTenantSaasPaymentMethodCommand(tenantId, "pm_b"),
            CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        await provider.Received(1).DetachSaasPaymentMethodAsync(
            Arg.Is<DetachSaasPaymentMethodRequest>(x => x.CustomerId == "cus_1" && x.PaymentMethodId == "pm_b"),
            Arg.Any<CancellationToken>());
    }
}
