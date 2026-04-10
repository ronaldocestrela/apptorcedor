using FluentAssertions;
using NSubstitute;
using SocioTorcedor.Modules.Membership.Application.Commands.UpdateMemberPlan;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Application.Tests.Support;
using SocioTorcedor.Modules.Membership.Domain.Entities;
using SocioTorcedor.Modules.Membership.Domain.ValueObjects;

namespace SocioTorcedor.Modules.Membership.Application.Tests.Commands;

public sealed class UpdateMemberPlanHandlerTests
{
    [Fact]
    public async Task When_plan_missing_returns_PlanNotFound()
    {
        var repo = Substitute.For<IMemberPlanRepository>();
        repo.GetTrackedByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((MemberPlan?)null);

        var handler = new UpdateMemberPlanHandler(repo, new ResolvedTenant(Guid.NewGuid()));
        var id = Guid.NewGuid();

        var r = await handler.Handle(
            new UpdateMemberPlanCommand(id, "N", null, 1m, null),
            CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Membership.PlanNotFound");
    }

    [Fact]
    public async Task When_name_conflict_returns_PlanNameConflict()
    {
        var plan = MemberPlan.Create("A", null, 1m, null, () => false);
        var repo = Substitute.For<IMemberPlanRepository>();
        repo.GetTrackedByIdAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);
        repo.ExistsByNameAsync("B", plan.Id, Arg.Any<CancellationToken>()).Returns(true);

        var handler = new UpdateMemberPlanHandler(repo, new ResolvedTenant(Guid.NewGuid()));

        var r = await handler.Handle(
            new UpdateMemberPlanCommand(plan.Id, "B", null, 1m, null),
            CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Membership.PlanNameConflict");
    }

    [Fact]
    public async Task Updates_and_persists()
    {
        var plan = MemberPlan.Create(
            "A",
            null,
            1m,
            new List<Vantagem> { Vantagem.Create("Old") },
            () => false);

        var repo = Substitute.For<IMemberPlanRepository>();
        repo.GetTrackedByIdAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);
        repo.ExistsByNameAsync("New", plan.Id, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new UpdateMemberPlanHandler(repo, new ResolvedTenant(Guid.NewGuid()));

        var r = await handler.Handle(
            new UpdateMemberPlanCommand(plan.Id, "New", "D", 99m, ["X"]),
            CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value!.Nome.Should().Be("New");
        r.Value.Preco.Should().Be(99m);
        r.Value.Vantagens.Should().ContainSingle(v => v.Descricao == "X");
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
