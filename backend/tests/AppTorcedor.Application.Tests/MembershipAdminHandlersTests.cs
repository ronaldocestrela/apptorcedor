using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Commands.UpdateMembershipStatus;
using AppTorcedor.Application.Modules.Administration.Queries.GetAdminMembershipDetail;
using AppTorcedor.Application.Modules.Administration.Queries.ListAdminMemberships;
using AppTorcedor.Application.Modules.Administration.Queries.ListMembershipHistory;
using AppTorcedor.Identity;

namespace AppTorcedor.Application.Tests;

public sealed class MembershipAdminHandlersTests
{
    [Fact]
    public async Task ListAdminMemberships_delegates_to_port()
    {
        var fake = new FakeMembershipAdministrationPort();
        var handler = new ListAdminMembershipsQueryHandler(fake);
        var page = await handler.Handle(new ListAdminMembershipsQuery(MembershipStatus.Ativo, Guid.NewGuid(), 2, 15), CancellationToken.None);
        Assert.Equal(0, page.TotalCount);
        Assert.Single(fake.ListCalls);
        Assert.Equal(MembershipStatus.Ativo, fake.ListCalls[0].Status);
        Assert.Equal(2, fake.ListCalls[0].Page);
        Assert.Equal(15, fake.ListCalls[0].PageSize);
    }

    [Fact]
    public async Task GetAdminMembershipDetail_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var fake = new FakeMembershipAdministrationPort();
        var handler = new GetAdminMembershipDetailQueryHandler(fake);
        var detail = await handler.Handle(new GetAdminMembershipDetailQuery(id), CancellationToken.None);
        Assert.Null(detail);
        Assert.Single(fake.DetailCalls);
        Assert.Equal(id, fake.DetailCalls[0]);
    }

    [Fact]
    public async Task ListMembershipHistory_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var fake = new FakeMembershipAdministrationPort();
        var handler = new ListMembershipHistoryQueryHandler(fake);
        var rows = await handler.Handle(new ListMembershipHistoryQuery(id, 25), CancellationToken.None);
        Assert.Empty(rows);
        Assert.Single(fake.HistoryCalls);
        Assert.Equal(id, fake.HistoryCalls[0].MembershipId);
        Assert.Equal(25, fake.HistoryCalls[0].Take);
    }

    [Fact]
    public async Task UpdateMembershipStatus_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var fake = new FakeMembershipAdministrationPort
        {
            UpdateResult = new MembershipStatusUpdateResult(true, null),
        };
        var handler = new UpdateMembershipStatusCommandHandler(fake);
        var result = await handler.Handle(new UpdateMembershipStatusCommand(id, MembershipStatus.Suspenso, "Fraude", actor), CancellationToken.None);
        Assert.True(result.Ok);
        Assert.Single(fake.UpdateCalls);
        Assert.Equal(id, fake.UpdateCalls[0].MembershipId);
        Assert.Equal(MembershipStatus.Suspenso, fake.UpdateCalls[0].Status);
        Assert.Equal("Fraude", fake.UpdateCalls[0].Reason);
        Assert.Equal(actor, fake.UpdateCalls[0].ActorUserId);
    }

    private sealed class FakeMembershipAdministrationPort : IMembershipAdministrationPort
    {
        public List<(MembershipStatus? Status, Guid? UserId, int Page, int PageSize)> ListCalls { get; } = [];
        public List<Guid> DetailCalls { get; } = [];
        public List<(Guid MembershipId, int Take)> HistoryCalls { get; } = [];
        public List<(Guid MembershipId, MembershipStatus Status, string Reason, Guid ActorUserId)> UpdateCalls { get; } = [];
        public List<(Guid MembershipId, MembershipStatus ToStatus, string Reason)> SystemTransitionCalls { get; } = [];

        public MembershipStatusUpdateResult UpdateResult { get; init; } = new(false, MembershipStatusUpdateError.NotFound);
        public MembershipStatusUpdateResult SystemTransitionResult { get; init; } = new(true, null);

        public Task<AdminMembershipListPageDto> ListMembershipsAsync(
            MembershipStatus? status,
            Guid? userId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            ListCalls.Add((status, userId, page, pageSize));
            return Task.FromResult(new AdminMembershipListPageDto(0, []));
        }

        public Task<AdminMembershipDetailDto?> GetMembershipByIdAsync(Guid membershipId, CancellationToken cancellationToken = default)
        {
            DetailCalls.Add(membershipId);
            return Task.FromResult<AdminMembershipDetailDto?>(null);
        }

        public Task<IReadOnlyList<MembershipHistoryEventDto>> ListHistoryAsync(
            Guid membershipId,
            int take,
            CancellationToken cancellationToken = default)
        {
            HistoryCalls.Add((membershipId, take));
            return Task.FromResult<IReadOnlyList<MembershipHistoryEventDto>>([]);
        }

        public Task<MembershipStatusUpdateResult> UpdateStatusAsync(
            Guid membershipId,
            MembershipStatus status,
            string reason,
            Guid actorUserId,
            CancellationToken cancellationToken = default)
        {
            UpdateCalls.Add((membershipId, status, reason, actorUserId));
            return Task.FromResult(UpdateResult);
        }

        public Task<MembershipStatusUpdateResult> ApplySystemMembershipTransitionAsync(
            Guid membershipId,
            MembershipStatus toStatus,
            string reason,
            CancellationToken cancellationToken = default)
        {
            SystemTransitionCalls.Add((membershipId, toStatus, reason));
            return Task.FromResult(SystemTransitionResult);
        }
    }
}
