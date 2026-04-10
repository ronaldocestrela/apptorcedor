using FluentAssertions;
using NSubstitute;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Domain.Entities;
using SocioTorcedor.Modules.Membership.Domain.Enums;
using SocioTorcedor.Modules.Membership.Domain.ValueObjects;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.Queries.GetMyMemberBilling;
using SocioTorcedor.Modules.Payments.Domain.Entities;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Application.Tests.Queries;

public sealed class GetMyMemberBillingHandlerTests
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
    public async Task When_user_missing_returns_Auth_Required()
    {
        var user = Substitute.For<ICurrentUserAccessor>();
        user.GetUserId().Returns((string?)null);
        var profileRepo = Substitute.For<IMemberProfileRepository>();
        var planRepo = Substitute.For<IMemberPlanRepository>();
        var payRepo = Substitute.For<IMemberTenantPaymentsRepository>();

        var handler = new GetMyMemberBillingHandler(user, profileRepo, planRepo, payRepo);

        var r = await handler.Handle(new GetMyMemberBillingQuery(), CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Auth.Required");
        await profileRepo.DidNotReceive()
            .GetTrackedByUserIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task When_no_profile_returns_null()
    {
        var user = Substitute.For<ICurrentUserAccessor>();
        user.GetUserId().Returns("u1");
        var profileRepo = Substitute.For<IMemberProfileRepository>();
        profileRepo.GetTrackedByUserIdAsync("u1", Arg.Any<CancellationToken>()).Returns((MemberProfile?)null);
        var planRepo = Substitute.For<IMemberPlanRepository>();
        var payRepo = Substitute.For<IMemberTenantPaymentsRepository>();

        var handler = new GetMyMemberBillingHandler(user, profileRepo, planRepo, payRepo);

        var r = await handler.Handle(new GetMyMemberBillingQuery(), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value.Should().BeNull();
        await payRepo.DidNotReceive()
            .GetActiveSubscriptionByMemberAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task When_no_subscription_returns_null()
    {
        var profile = TestProfile();
        var user = Substitute.For<ICurrentUserAccessor>();
        user.GetUserId().Returns("user-1");
        var profileRepo = Substitute.For<IMemberProfileRepository>();
        profileRepo.GetTrackedByUserIdAsync("user-1", Arg.Any<CancellationToken>()).Returns(profile);
        var planRepo = Substitute.For<IMemberPlanRepository>();
        var payRepo = Substitute.For<IMemberTenantPaymentsRepository>();
        payRepo.GetActiveSubscriptionByMemberAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns((MemberBillingSubscription?)null);

        var handler = new GetMyMemberBillingHandler(user, profileRepo, planRepo, payRepo);

        var r = await handler.Handle(new GetMyMemberBillingQuery(), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value.Should().BeNull();
        await planRepo.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task When_subscription_and_plan_exist_returns_plan_name()
    {
        var profile = TestProfile();
        var plan = MemberPlan.Create("Ouro", null, 99m, null, () => false);
        var sub = MemberBillingSubscription.Start(
            profile.Id,
            plan.Id,
            99m,
            "BRL",
            PaymentMethodKind.Pix,
            null,
            null,
            BillingSubscriptionStatus.Active,
            null);

        var user = Substitute.For<ICurrentUserAccessor>();
        user.GetUserId().Returns("user-1");
        var profileRepo = Substitute.For<IMemberProfileRepository>();
        profileRepo.GetTrackedByUserIdAsync("user-1", Arg.Any<CancellationToken>()).Returns(profile);
        var planRepo = Substitute.For<IMemberPlanRepository>();
        planRepo.GetByIdAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);
        var payRepo = Substitute.For<IMemberTenantPaymentsRepository>();
        payRepo.GetActiveSubscriptionByMemberAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(sub);

        var handler = new GetMyMemberBillingHandler(user, profileRepo, planRepo, payRepo);

        var r = await handler.Handle(new GetMyMemberBillingQuery(), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value.Should().NotBeNull();
        r.Value!.PlanName.Should().Be("Ouro");
        r.Value.MemberPlanId.Should().Be(plan.Id);
    }

    [Fact]
    public async Task When_plan_missing_returns_null_plan_name()
    {
        var profile = TestProfile();
        var planId = Guid.NewGuid();
        var sub = MemberBillingSubscription.Start(
            profile.Id,
            planId,
            10m,
            "BRL",
            PaymentMethodKind.Pix,
            null,
            null,
            BillingSubscriptionStatus.Active,
            null);

        var user = Substitute.For<ICurrentUserAccessor>();
        user.GetUserId().Returns("user-1");
        var profileRepo = Substitute.For<IMemberProfileRepository>();
        profileRepo.GetTrackedByUserIdAsync("user-1", Arg.Any<CancellationToken>()).Returns(profile);
        var planRepo = Substitute.For<IMemberPlanRepository>();
        planRepo.GetByIdAsync(planId, Arg.Any<CancellationToken>()).Returns((MemberPlan?)null);
        var payRepo = Substitute.For<IMemberTenantPaymentsRepository>();
        payRepo.GetActiveSubscriptionByMemberAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(sub);

        var handler = new GetMyMemberBillingHandler(user, profileRepo, planRepo, payRepo);

        var r = await handler.Handle(new GetMyMemberBillingQuery(), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value!.PlanName.Should().BeNull();
        r.Value.MemberPlanId.Should().Be(planId);
    }
}
