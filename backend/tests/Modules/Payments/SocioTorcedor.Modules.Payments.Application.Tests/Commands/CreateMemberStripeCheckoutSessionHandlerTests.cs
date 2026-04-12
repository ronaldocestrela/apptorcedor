using FluentAssertions;
using NSubstitute;
using SocioTorcedor.BuildingBlocks.Application.Payments;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Domain.Entities;
using SocioTorcedor.Modules.Membership.Domain.Enums;
using SocioTorcedor.Modules.Membership.Domain.ValueObjects;
using SocioTorcedor.Modules.Payments.Application.Commands.CreateMemberStripeCheckoutSession;
using SocioTorcedor.Modules.Payments.Application.Contracts;

namespace SocioTorcedor.Modules.Payments.Application.Tests.Commands;

public sealed class CreateMemberStripeCheckoutSessionHandlerTests
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
    public async Task When_success_url_invalid_returns_InvalidSuccessUrl()
    {
        var handler = CreateHandler(out _, out _, out _, out _, out _, out _, out _);
        var r = await handler.Handle(
            new CreateMemberStripeCheckoutSessionCommand(
                Guid.NewGuid(),
                SuccessUrl: "/bad",
                CancelUrl: "https://app.example.com/cancel"),
            CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Payments.InvalidSuccessUrl");
    }

    [Fact]
    public async Task When_cancel_url_invalid_returns_InvalidCancelUrl()
    {
        var handler = CreateHandler(out _, out _, out _, out _, out _, out _, out _);
        var r = await handler.Handle(
            new CreateMemberStripeCheckoutSessionCommand(
                Guid.NewGuid(),
                SuccessUrl: "https://app.example.com/ok",
                CancelUrl: "not-a-url"),
            CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Payments.InvalidCancelUrl");
    }

    [Fact]
    public async Task When_stripe_disabled_returns_Stripe_Disabled()
    {
        var handler = CreateHandler(out _, out _, out _, out var gateway, out _, out _, out _);
        gateway.IsStripeEnabled.Returns(false);

        var r = await handler.Handle(
            new CreateMemberStripeCheckoutSessionCommand(
                Guid.NewGuid(),
                SuccessUrl: "https://app.example.com/ok",
                CancelUrl: "https://app.example.com/cancel"),
            CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Payments.Stripe.Disabled");
    }

    [Fact]
    public async Task When_valid_calls_CreateCheckoutSession_with_metadata_including_recurring_amount()
    {
        var profile = TestProfile();
        var plan = MemberPlan.Create("Ouro", null, 99.5m, null, () => false);
        var tenantId = Guid.NewGuid();

        var handler = CreateHandler(out var user, out var tenant, out var profileRepo, out var gateway, out var memberGw, out var payProvider, out var planRepo);
        gateway.IsStripeEnabled.Returns(true);
        user.GetUserId().Returns("user-1");
        tenant.TenantId.Returns(tenantId);
        profileRepo.GetTrackedByUserIdAsync("user-1", Arg.Any<CancellationToken>()).Returns(profile);
        planRepo.GetByIdAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);
        memberGw.EnsureMemberGatewayReadyForChargeAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(SocioTorcedor.BuildingBlocks.Shared.Results.Result.Ok());

        payProvider
            .CreateCheckoutSessionAsync(Arg.Any<CreateCheckoutSessionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateCheckoutSessionResult("cs_test", "https://stripe.test/checkout"));

        var r = await handler.Handle(
            new CreateMemberStripeCheckoutSessionCommand(
                plan.Id,
                SuccessUrl: "https://app.example.com/ok",
                CancelUrl: "https://app.example.com/cancel"),
            CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value!.SessionId.Should().Be("cs_test");
        r.Value.Url.Should().Be("https://stripe.test/checkout");

        await payProvider.Received(1).CreateCheckoutSessionAsync(
            Arg.Is<CreateCheckoutSessionRequest>(x =>
                x.Context == PaymentProviderContext.Member
                && x.SuccessUrl == "https://app.example.com/ok"
                && x.CancelUrl == "https://app.example.com/cancel"
                && x.Metadata != null
                && x.Metadata["tenant_id"] == tenantId.ToString("D")
                && x.Metadata["member_profile_id"] == profile.Id.ToString("D")
                && x.Metadata["member_plan_id"] == plan.Id.ToString("D")
                && x.Metadata["recurring_amount_brl"] == "99.5"),
            Arg.Any<CancellationToken>());
    }

    private static CreateMemberStripeCheckoutSessionHandler CreateHandler(
        out ICurrentUserAccessor user,
        out ICurrentTenantContext tenant,
        out IMemberProfileRepository profileRepo,
        out IPaymentsGatewayMetadata gateway,
        out IMemberPaymentGatewayService memberGw,
        out IPaymentProvider payProvider,
        out IMemberPlanRepository planRepo)
    {
        user = Substitute.For<ICurrentUserAccessor>();
        tenant = Substitute.For<ICurrentTenantContext>();
        profileRepo = Substitute.For<IMemberProfileRepository>();
        planRepo = Substitute.For<IMemberPlanRepository>();
        gateway = Substitute.For<IPaymentsGatewayMetadata>();
        memberGw = Substitute.For<IMemberPaymentGatewayService>();
        payProvider = Substitute.For<IPaymentProvider>();

        return new CreateMemberStripeCheckoutSessionHandler(
            user,
            tenant,
            profileRepo,
            planRepo,
            gateway,
            memberGw,
            payProvider);
    }
}
