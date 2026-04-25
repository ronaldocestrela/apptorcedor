using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Torcedor.Commands.RedeemMyTicket;
using AppTorcedor.Application.Modules.Torcedor.Commands.RequestMyTicket;
using AppTorcedor.Application.Modules.Torcedor.Queries.GetMyTicket;
using AppTorcedor.Application.Modules.Torcedor.Queries.ListMyTickets;
using AppTorcedor.Application.Modules.Torcedor.Queries.ListTorcedorGames;

namespace AppTorcedor.Application.Tests;

public sealed class TorcedorGamesTicketsHandlersTests
{
    [Fact]
    public async Task ListTorcedorGames_delegates_to_port()
    {
        var fake = new FakeGamesPort();
        var handler = new ListTorcedorGamesQueryHandler(fake);
        var page = await handler.Handle(new ListTorcedorGamesQuery("riv", 2, 15), CancellationToken.None);
        Assert.Equal(0, page.TotalCount);
        Assert.Single(fake.ListCalls);
        Assert.Equal("riv", fake.ListCalls[0].Search);
        Assert.Equal(2, fake.ListCalls[0].Page);
        Assert.Equal(15, fake.ListCalls[0].PageSize);
    }

    [Fact]
    public async Task ListMyTickets_delegates_to_port()
    {
        var uid = Guid.NewGuid();
        var gid = Guid.NewGuid();
        var fake = new FakeTicketsPort();
        var handler = new ListMyTicketsQueryHandler(fake);
        var page = await handler.Handle(new ListMyTicketsQuery(uid, gid, "Purchased", 1, 10), CancellationToken.None);
        Assert.Equal(0, page.TotalCount);
        Assert.Single(fake.ListCalls);
        Assert.Equal(uid, fake.ListCalls[0].UserId);
        Assert.Equal(gid, fake.ListCalls[0].GameId);
        Assert.Equal("Purchased", fake.ListCalls[0].Status);
    }

    [Fact]
    public async Task GetMyTicket_delegates_to_port()
    {
        var uid = Guid.NewGuid();
        var tid = Guid.NewGuid();
        var fake = new FakeTicketsPort();
        var handler = new GetMyTicketQueryHandler(fake);
        var d = await handler.Handle(new GetMyTicketQuery(uid, tid), CancellationToken.None);
        Assert.Null(d);
        Assert.Single(fake.GetCalls);
        Assert.Equal((uid, tid), fake.GetCalls[0]);
    }

    [Fact]
    public async Task RequestMyTicket_delegates_to_port()
    {
        var uid = Guid.NewGuid();
        var gid = Guid.NewGuid();
        var tid = Guid.NewGuid();
        var fake = new FakeTicketsPort { RequestResult = new TicketReserveResult(tid, null) };
        var handler = new RequestMyTicketCommandHandler(fake);
        var r = await handler.Handle(new RequestMyTicketCommand(uid, gid), CancellationToken.None);
        Assert.True(r.Ok);
        Assert.Equal(tid, r.TicketId);
        Assert.Single(fake.RequestCalls);
        Assert.Equal((uid, gid), fake.RequestCalls[0]);
    }

    [Fact]
    public async Task RedeemMyTicket_delegates_to_port()
    {
        var uid = Guid.NewGuid();
        var tid = Guid.NewGuid();
        var fake = new FakeTicketsPort { RedeemResult = TicketMutationResult.Success() };
        var handler = new RedeemMyTicketCommandHandler(fake);
        var r = await handler.Handle(new RedeemMyTicketCommand(uid, tid), CancellationToken.None);
        Assert.True(r.Ok);
        Assert.Single(fake.RedeemCalls);
        Assert.Equal((uid, tid), fake.RedeemCalls[0]);
    }

    private sealed class FakeGamesPort : IGameTorcedorReadPort
    {
        public List<(string? Search, int Page, int PageSize)> ListCalls { get; } = [];

        public Task<TorcedorGameListPageDto> ListActiveGamesAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            ListCalls.Add((search, page, pageSize));
            return Task.FromResult(new TorcedorGameListPageDto(0, []));
        }
    }

    private sealed class FakeTicketsPort : ITicketTorcedorPort
    {
        public List<(Guid UserId, Guid? GameId, string? Status, int Page, int PageSize)> ListCalls { get; } = [];
        public List<(Guid UserId, Guid TicketId)> GetCalls { get; } = [];
        public List<(Guid UserId, Guid TicketId)> RedeemCalls { get; } = [];
        public List<(Guid UserId, Guid GameId)> RequestCalls { get; } = [];

        public TicketMutationResult RedeemResult { get; init; } = TicketMutationResult.Fail(TicketMutationError.NotFound);
        public TicketReserveResult RequestResult { get; init; } = new(null, TicketMutationError.MembershipNotActive);

        public Task<TicketReserveResult> RequestMyTicketAsync(
            Guid userId,
            Guid gameId,
            CancellationToken cancellationToken = default)
        {
            RequestCalls.Add((userId, gameId));
            return Task.FromResult(RequestResult);
        }

        public Task<TorcedorTicketListPageDto> ListMyTicketsAsync(
            Guid userId,
            Guid? gameId,
            string? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            ListCalls.Add((userId, gameId, status, page, pageSize));
            return Task.FromResult(new TorcedorTicketListPageDto(0, []));
        }

        public Task<TorcedorTicketDetailDto?> GetMyTicketAsync(Guid userId, Guid ticketId, CancellationToken cancellationToken = default)
        {
            GetCalls.Add((userId, ticketId));
            return Task.FromResult<TorcedorTicketDetailDto?>(null);
        }

        public Task<TicketMutationResult> RedeemMyTicketAsync(Guid userId, Guid ticketId, CancellationToken cancellationToken = default)
        {
            RedeemCalls.Add((userId, ticketId));
            return Task.FromResult(RedeemResult);
        }
    }
}
