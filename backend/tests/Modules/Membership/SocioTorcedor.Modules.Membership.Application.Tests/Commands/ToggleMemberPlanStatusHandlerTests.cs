using FluentAssertions;
using NSubstitute;
using SocioTorcedor.Modules.Membership.Application.Commands.ToggleMemberPlanStatus;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Application.Tests.Support;
using SocioTorcedor.Modules.Membership.Domain.Entities;

namespace SocioTorcedor.Modules.Membership.Application.Tests.Commands;

public sealed class ToggleMemberPlanStatusHandlerTests
{
    [Fact]
    public async Task When_plan_missing_returns_PlanNotFound()
    {
        var repo = Substitute.For<IMemberPlanRepository>();
        repo.GetTrackedByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((MemberPlan?)null);

        var handler = new ToggleMemberPlanStatusHandler(repo, new ResolvedTenant(Guid.NewGuid()));

        var r = await handler.Handle(new ToggleMemberPlanStatusCommand(Guid.NewGuid()), CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Membership.PlanNotFound");
    }

    [Fact]
    public async Task Toggles_and_saves()
    {
        var plan = MemberPlan.Create("P", null, 1m, null, () => false);
        var repo = Substitute.For<IMemberPlanRepository>();
        repo.GetTrackedByIdAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);

        var handler = new ToggleMemberPlanStatusHandler(repo, new ResolvedTenant(Guid.NewGuid()));

        var r = await handler.Handle(new ToggleMemberPlanStatusCommand(plan.Id), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value!.IsActive.Should().BeFalse();
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
