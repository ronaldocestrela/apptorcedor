using FluentAssertions;
using NSubstitute;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Application.Queries.GetMemberPlanById;
using SocioTorcedor.Modules.Membership.Application.Tests.Support;
using SocioTorcedor.Modules.Membership.Domain.Entities;

namespace SocioTorcedor.Modules.Membership.Application.Tests.Queries;

public sealed class GetMemberPlanByIdHandlerTests
{
    [Fact]
    public async Task When_tenant_unresolved_returns_Tenant_Required()
    {
        var repo = Substitute.For<IMemberPlanRepository>();
        var handler = new GetMemberPlanByIdHandler(repo, new UnresolvedTenant());

        var r = await handler.Handle(new GetMemberPlanByIdQuery(Guid.NewGuid()), CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Tenant.Required");
    }

    [Fact]
    public async Task When_missing_returns_PlanNotFound()
    {
        var repo = Substitute.For<IMemberPlanRepository>();
        repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((MemberPlan?)null);

        var handler = new GetMemberPlanByIdHandler(repo, new ResolvedTenant(Guid.NewGuid()));

        var r = await handler.Handle(new GetMemberPlanByIdQuery(Guid.NewGuid()), CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Membership.PlanNotFound");
    }

    [Fact]
    public async Task Returns_dto()
    {
        var plan = MemberPlan.Create("Z", "z", 5m, null, () => false);
        var repo = Substitute.For<IMemberPlanRepository>();
        repo.GetByIdAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);

        var handler = new GetMemberPlanByIdHandler(repo, new ResolvedTenant(Guid.NewGuid()));

        var r = await handler.Handle(new GetMemberPlanByIdQuery(plan.Id), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value!.Id.Should().Be(plan.Id);
        r.Value.Nome.Should().Be("Z");
    }
}
