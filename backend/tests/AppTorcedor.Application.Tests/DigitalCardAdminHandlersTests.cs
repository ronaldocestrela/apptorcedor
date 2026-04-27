using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Commands.InvalidateDigitalCard;
using AppTorcedor.Application.Modules.Administration.Commands.IssueDigitalCard;
using AppTorcedor.Application.Modules.Administration.Commands.RegenerateDigitalCard;
using AppTorcedor.Application.Modules.Administration.Queries.GetAdminDigitalCardDetail;
using AppTorcedor.Application.Modules.Administration.Queries.ListAdminDigitalCards;
using AppTorcedor.Application.Modules.Administration.Queries.ListDigitalCardIssueCandidates;

namespace AppTorcedor.Application.Tests;

public sealed class DigitalCardAdminHandlersTests
{
    [Fact]
    public async Task ListAdminDigitalCards_delegates_to_port()
    {
        var fake = new FakeDigitalCardPort();
        var handler = new ListAdminDigitalCardsQueryHandler(fake);
        var uid = Guid.NewGuid();
        var page = await handler.Handle(new ListAdminDigitalCardsQuery(uid, null, "Active", 2, 15), CancellationToken.None);
        Assert.Equal(0, page.TotalCount);
        Assert.Single(fake.ListCalls);
        Assert.Equal(uid, fake.ListCalls[0].UserId);
        Assert.Equal("Active", fake.ListCalls[0].Status);
        Assert.Equal(2, fake.ListCalls[0].Page);
        Assert.Equal(15, fake.ListCalls[0].PageSize);
    }

    [Fact]
    public async Task ListDigitalCardIssueCandidates_delegates_to_port()
    {
        var fake = new FakeDigitalCardPort();
        var handler = new ListDigitalCardIssueCandidatesQueryHandler(fake);
        var page = await handler.Handle(new ListDigitalCardIssueCandidatesQuery(3, 10), CancellationToken.None);
        Assert.Equal(0, page.TotalCount);
        Assert.Empty(page.Items);
        Assert.Single(fake.IssueCandidatesCalls);
        Assert.Equal(3, fake.IssueCandidatesCalls[0].Page);
        Assert.Equal(10, fake.IssueCandidatesCalls[0].PageSize);
    }

    [Fact]
    public async Task GetAdminDigitalCardDetail_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var fake = new FakeDigitalCardPort();
        var handler = new GetAdminDigitalCardDetailQueryHandler(fake);
        var detail = await handler.Handle(new GetAdminDigitalCardDetailQuery(id), CancellationToken.None);
        Assert.Null(detail);
        Assert.Single(fake.DetailCalls);
        Assert.Equal(id, fake.DetailCalls[0]);
    }

    [Fact]
    public async Task IssueDigitalCard_delegates_to_port()
    {
        var mid = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var fake = new FakeDigitalCardPort { MutationResult = new DigitalCardMutationResult(true, null) };
        var handler = new IssueDigitalCardCommandHandler(fake);
        var r = await handler.Handle(new IssueDigitalCardCommand(mid, actor), CancellationToken.None);
        Assert.True(r.Ok);
        Assert.Single(fake.IssueCalls);
        Assert.Equal(mid, fake.IssueCalls[0].MembershipId);
        Assert.Equal(actor, fake.IssueCalls[0].ActorUserId);
    }

    [Fact]
    public async Task RegenerateDigitalCard_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var fake = new FakeDigitalCardPort { MutationResult = new DigitalCardMutationResult(true, null) };
        var handler = new RegenerateDigitalCardCommandHandler(fake);
        var r = await handler.Handle(new RegenerateDigitalCardCommand(id, "troca", actor), CancellationToken.None);
        Assert.True(r.Ok);
        Assert.Single(fake.RegenerateCalls);
        Assert.Equal(id, fake.RegenerateCalls[0].DigitalCardId);
        Assert.Equal("troca", fake.RegenerateCalls[0].Reason);
    }

    [Fact]
    public async Task InvalidateDigitalCard_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var fake = new FakeDigitalCardPort { MutationResult = new DigitalCardMutationResult(true, null) };
        var handler = new InvalidateDigitalCardCommandHandler(fake);
        var r = await handler.Handle(new InvalidateDigitalCardCommand(id, "fraude", actor), CancellationToken.None);
        Assert.True(r.Ok);
        Assert.Single(fake.InvalidateCalls);
        Assert.Equal(id, fake.InvalidateCalls[0].DigitalCardId);
        Assert.Equal("fraude", fake.InvalidateCalls[0].Reason);
    }

    private sealed class FakeDigitalCardPort : IDigitalCardAdministrationPort
    {
        public DigitalCardMutationResult MutationResult { get; init; } = new(false, DigitalCardMutationError.NotFound);

        public List<(Guid? UserId, Guid? MembershipId, string? Status, int Page, int PageSize)> ListCalls { get; } = [];
        public List<(int Page, int PageSize)> IssueCandidatesCalls { get; } = [];
        public List<Guid> DetailCalls { get; } = [];
        public List<(Guid MembershipId, Guid ActorUserId)> IssueCalls { get; } = [];
        public List<(Guid DigitalCardId, string? Reason, Guid ActorUserId)> RegenerateCalls { get; } = [];
        public List<(Guid DigitalCardId, string Reason, Guid ActorUserId)> InvalidateCalls { get; } = [];

        public Task<AdminDigitalCardListPageDto> ListDigitalCardsAsync(
            Guid? userId,
            Guid? membershipId,
            string? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            ListCalls.Add((userId, membershipId, status, page, pageSize));
            return Task.FromResult(new AdminDigitalCardListPageDto(0, []));
        }

        public Task<AdminDigitalCardIssueCandidatesPageDto> ListDigitalCardIssueCandidatesAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            IssueCandidatesCalls.Add((page, pageSize));
            return Task.FromResult(new AdminDigitalCardIssueCandidatesPageDto(0, []));
        }

        public Task<AdminDigitalCardDetailDto?> GetDigitalCardByIdAsync(Guid digitalCardId, CancellationToken cancellationToken = default)
        {
            DetailCalls.Add(digitalCardId);
            return Task.FromResult<AdminDigitalCardDetailDto?>(null);
        }

        public Task<DigitalCardMutationResult> IssueDigitalCardAsync(
            Guid membershipId,
            Guid actorUserId,
            CancellationToken cancellationToken = default)
        {
            IssueCalls.Add((membershipId, actorUserId));
            return Task.FromResult(MutationResult);
        }

        public Task<DigitalCardMutationResult> RegenerateDigitalCardAsync(
            Guid digitalCardId,
            string? reason,
            Guid actorUserId,
            CancellationToken cancellationToken = default)
        {
            RegenerateCalls.Add((digitalCardId, reason, actorUserId));
            return Task.FromResult(MutationResult);
        }

        public Task<DigitalCardMutationResult> InvalidateDigitalCardAsync(
            Guid digitalCardId,
            string reason,
            Guid actorUserId,
            CancellationToken cancellationToken = default)
        {
            InvalidateCalls.Add((digitalCardId, reason, actorUserId));
            return Task.FromResult(MutationResult);
        }
    }
}
