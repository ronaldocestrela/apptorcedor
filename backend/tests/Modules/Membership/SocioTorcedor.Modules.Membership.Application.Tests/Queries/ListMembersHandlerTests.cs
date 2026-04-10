using FluentAssertions;
using NSubstitute;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Application.Queries.ListMembers;
using SocioTorcedor.Modules.Membership.Application.Tests.Support;
using SocioTorcedor.Modules.Membership.Domain.Entities;

namespace SocioTorcedor.Modules.Membership.Application.Tests.Queries;

public sealed class ListMembersHandlerTests
{
    [Fact]
    public async Task Returns_paged_dtos()
    {
        var repo = Substitute.For<IMemberProfileRepository>();
        repo.ListAsync(2, 10, null, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<MemberProfile>(Array.Empty<MemberProfile>(), 0, 2, 10));

        var handler = new ListMembersHandler(repo, new ResolvedTenant(Guid.NewGuid()));

        var r = await handler.Handle(new ListMembersQuery(2, 10), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value!.Page.Should().Be(2);
        r.Value.PageSize.Should().Be(10);
        r.Value.TotalCount.Should().Be(0);
        r.Value.Items.Should().BeEmpty();
    }
}
