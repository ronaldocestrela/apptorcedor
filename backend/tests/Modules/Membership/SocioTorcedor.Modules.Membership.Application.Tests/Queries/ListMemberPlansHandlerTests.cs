using FluentAssertions;
using NSubstitute;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Application.Queries.ListMemberPlans;
using SocioTorcedor.Modules.Membership.Application.Tests.Support;
using SocioTorcedor.Modules.Membership.Domain.Entities;

namespace SocioTorcedor.Modules.Membership.Application.Tests.Queries;

public sealed class ListMemberPlansHandlerTests
{
    [Fact]
    public async Task When_tenant_unresolved_returns_Tenant_Required()
    {
        var repo = Substitute.For<IMemberPlanRepository>();
        var handler = new ListMemberPlansHandler(repo, new UnresolvedTenant());

        var r = await handler.Handle(new ListMemberPlansQuery(1, 20), CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Tenant.Required");
    }

    [Fact]
    public async Task Maps_page_to_dtos()
    {
        var p1 = MemberPlan.Create("A", null, 1m, null, () => false);
        var p2 = MemberPlan.Create("B", null, 2m, null, () => false);

        var repo = Substitute.For<IMemberPlanRepository>();
        repo.ListAsync(1, 20, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<MemberPlan>(new List<MemberPlan> { p1, p2 }, 2, 1, 20));

        var handler = new ListMemberPlansHandler(repo, new ResolvedTenant(Guid.NewGuid()));

        var r = await handler.Handle(new ListMemberPlansQuery(1, 20), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value!.Items.Should().HaveCount(2);
        r.Value.TotalCount.Should().Be(2);
        r.Value.Items[0].Nome.Should().Be("A");
    }
}
