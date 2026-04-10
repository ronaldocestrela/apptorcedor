using FluentAssertions;
using NSubstitute;
using SocioTorcedor.Modules.Membership.Application.Commands.CreateMemberPlan;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Application.Tests.Support;
using SocioTorcedor.Modules.Membership.Domain.Entities;

namespace SocioTorcedor.Modules.Membership.Application.Tests.Commands;

public sealed class CreateMemberPlanHandlerTests
{
    [Fact]
    public async Task When_tenant_unresolved_returns_Tenant_Required()
    {
        var repo = Substitute.For<IMemberPlanRepository>();
        var handler = new CreateMemberPlanHandler(repo, new UnresolvedTenant());

        var r = await handler.Handle(
            new CreateMemberPlanCommand("Gold", null, 10m, ["A"]),
            CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Tenant.Required");
        await repo.DidNotReceive().AddAsync(Arg.Any<MemberPlan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task When_name_taken_returns_PlanNameConflict()
    {
        var repo = Substitute.For<IMemberPlanRepository>();
        repo.ExistsByNameAsync("Gold", null, Arg.Any<CancellationToken>()).Returns(true);

        var handler = new CreateMemberPlanHandler(repo, new ResolvedTenant(Guid.NewGuid()));

        var r = await handler.Handle(
            new CreateMemberPlanCommand("Gold", null, 10m, null),
            CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Membership.PlanNameConflict");
    }

    [Fact]
    public async Task Persists_plan_and_returns_dto_with_vantagens()
    {
        var repo = Substitute.For<IMemberPlanRepository>();
        repo.ExistsByNameAsync("Silver", null, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new CreateMemberPlanHandler(repo, new ResolvedTenant(Guid.NewGuid()));

        var r = await handler.Handle(
            new CreateMemberPlanCommand("Silver", "Desc", 50m, ["V1", "V2"]),
            CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value!.Nome.Should().Be("Silver");
        r.Value.Vantagens.Should().HaveCount(2);
        r.Value.Vantagens[0].Descricao.Should().Be("V1");
        await repo.Received(1).AddAsync(Arg.Any<MemberPlan>(), Arg.Any<CancellationToken>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
