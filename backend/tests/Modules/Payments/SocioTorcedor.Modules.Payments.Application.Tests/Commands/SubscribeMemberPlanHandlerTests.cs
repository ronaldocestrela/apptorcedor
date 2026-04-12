using FluentAssertions;
using NSubstitute;
using SocioTorcedor.BuildingBlocks.Application.Payments;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Domain.Entities;
using SocioTorcedor.Modules.Membership.Domain.Enums;
using SocioTorcedor.Modules.Membership.Domain.ValueObjects;
using SocioTorcedor.Modules.Payments.Application.Commands.SubscribeMemberPlan;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Domain.Entities;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Application.Tests.Commands;

public sealed class SubscribeMemberPlanHandlerTests
{
    private static MemberProfile TestProfile(string userId = "user-1") =>
        MemberProfile.Create(
            userId,
            Cpf.Create("39053344705"),
            new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender.Male,
            "11999999999",
            Address.Create("Rua A", "100", null, "Centro", "São Paulo", "SP", "01310100"),
            () => false);

    [Fact]
    public async Task When_stripe_enabled_and_existing_subscription_cancels_and_creates_new_plan()
    {
        var profile = TestProfile();
        var oldPlan = MemberPlan.Create("Bronze", null, 50m, null, () => false);
        var newPlan = MemberPlan.Create("Ouro", null, 99m, null, () => false);

        var existingSub = MemberBillingSubscription.Start(
            profile.Id,
            oldPlan.Id,
            50m,
            "BRL",
            PaymentMethodKind.Pix,
            externalCustomerId: "cus_prev",
            externalSubscriptionId: "sub_0123456789abcdef0123456789abcdef",
            BillingSubscriptionStatus.Active,
            DateTime.UtcNow.AddMonths(1));

        var tenantId = Guid.NewGuid();

        var user = Substitute.For<ICurrentUserAccessor>();
        user.GetUserId().Returns("user-1");

        var tenant = Substitute.For<ICurrentTenantContext>();
        tenant.TenantId.Returns(tenantId);

        var profileRepo = Substitute.For<IMemberProfileRepository>();
        profileRepo.GetTrackedByUserIdAsync("user-1", Arg.Any<CancellationToken>()).Returns(profile);

        var planRepo = Substitute.For<IMemberPlanRepository>();
        planRepo.GetByIdAsync(newPlan.Id, Arg.Any<CancellationToken>()).Returns(newPlan);

        var payRepo = Substitute.For<IMemberTenantPaymentsRepository>();
        payRepo.GetActiveSubscriptionByMemberAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(existingSub);

        var gateway = Substitute.For<IPaymentsGatewayMetadata>();
        gateway.IsStripeEnabled.Returns(true);

        var memberGateway = Substitute.For<IMemberPaymentGatewayService>();
        memberGateway
            .EnsureMemberGatewayReadyForChargeAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var paymentProvider = Substitute.For<IPaymentProvider>();
        paymentProvider
            .CreateSubscriptionAsync(Arg.Any<CreateSubscriptionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateSubscriptionResult("cus_new", "sub_new", "active"));

        var handler = new SubscribeMemberPlanHandler(
            user,
            tenant,
            profileRepo,
            planRepo,
            payRepo,
            gateway,
            memberGateway,
            paymentProvider);

        var r = await handler.Handle(
            new SubscribeMemberPlanCommand(newPlan.Id, PaymentMethodKind.Pix),
            CancellationToken.None);

        r.IsSuccess.Should().BeTrue();

        await paymentProvider.Received(1).CancelAsync(
            PaymentProviderContext.Member,
            "sub_0123456789abcdef0123456789abcdef",
            null,
            "cancel:sub_0123456789abcdef0123456789abcdef",
            Arg.Any<CancellationToken>());

        await paymentProvider.Received(1).CreateSubscriptionAsync(
            Arg.Is<CreateSubscriptionRequest>(x =>
                x.Context == PaymentProviderContext.Member
                && x.ConnectedAccountId == null
                && x.ProductName == newPlan.Nome
                && x.AdditionalMetadata != null
                && x.AdditionalMetadata!["tenant_id"] == tenantId.ToString("D")),
            Arg.Any<CancellationToken>());

        existingSub.Status.Should().Be(BillingSubscriptionStatus.Canceled);

        await payRepo.Received(1).AddSubscriptionAsync(Arg.Any<MemberBillingSubscription>(), Arg.Any<CancellationToken>());
        await payRepo.Received(1).AddInvoiceAsync(Arg.Any<MemberBillingInvoice>(), Arg.Any<CancellationToken>());
        await payRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
