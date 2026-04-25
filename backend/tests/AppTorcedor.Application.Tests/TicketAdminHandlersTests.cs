using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Commands.PurchaseAdminTicket;
using AppTorcedor.Application.Modules.Administration.Commands.RedeemAdminTicket;
using AppTorcedor.Application.Modules.Administration.Commands.ReserveAdminTicket;
using AppTorcedor.Application.Modules.Administration.Commands.SyncAdminTicket;
using AppTorcedor.Application.Modules.Administration.Commands.UpdateAdminTicketRequestStatus;
using AppTorcedor.Application.Modules.Administration.Queries.GetAdminTicket;
using AppTorcedor.Application.Modules.Administration.Queries.ListAdminTickets;

namespace AppTorcedor.Application.Tests;

public sealed class TicketAdminHandlersTests
{
    [Fact]
    public async Task ListAdminTickets_delegates_to_port()
    {
        var uid = Guid.NewGuid();
        var gid = Guid.NewGuid();
        var fake = new FakeTicketPort();
        var handler = new ListAdminTicketsQueryHandler(fake);
        var page = await handler.Handle(
            new ListAdminTicketsQuery(uid, gid, "Reserved", "Issued", 3, 10),
            CancellationToken.None);
        Assert.Equal(0, page.TotalCount);
        Assert.Single(fake.ListCalls);
        Assert.Equal(uid, fake.ListCalls[0].UserId);
        Assert.Equal(gid, fake.ListCalls[0].GameId);
        Assert.Equal("Reserved", fake.ListCalls[0].Status);
        Assert.Equal("Issued", fake.ListCalls[0].RequestStatus);
    }

    [Fact]
    public async Task GetAdminTicket_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var fake = new FakeTicketPort();
        var handler = new GetAdminTicketQueryHandler(fake);
        var detail = await handler.Handle(new GetAdminTicketQuery(id), CancellationToken.None);
        Assert.Null(detail);
        Assert.Single(fake.DetailCalls);
        Assert.Equal(id, fake.DetailCalls[0]);
    }

    [Fact]
    public async Task ReserveAdminTicket_delegates_to_port()
    {
        var uid = Guid.NewGuid();
        var gid = Guid.NewGuid();
        var tid = Guid.NewGuid();
        var fake = new FakeTicketPort { ReserveResult = new TicketReserveResult(tid, null) };
        var handler = new ReserveAdminTicketCommandHandler(fake);
        var r = await handler.Handle(new ReserveAdminTicketCommand(uid, gid), CancellationToken.None);
        Assert.True(r.Ok);
        Assert.Equal(tid, r.TicketId);
        Assert.Single(fake.ReserveCalls);
    }

    [Fact]
    public async Task Purchase_sync_redeem_delegate_to_port()
    {
        var tid = Guid.NewGuid();
        var fake = new FakeTicketPort { Mutation = TicketMutationResult.Success() };
        Assert.True((await new PurchaseAdminTicketCommandHandler(fake).Handle(new PurchaseAdminTicketCommand(tid), CancellationToken.None)).Ok);
        Assert.True((await new SyncAdminTicketCommandHandler(fake).Handle(new SyncAdminTicketCommand(tid), CancellationToken.None)).Ok);
        Assert.True((await new RedeemAdminTicketCommandHandler(fake).Handle(new RedeemAdminTicketCommand(tid), CancellationToken.None)).Ok);
        Assert.Single(fake.PurchaseCalls);
        Assert.Single(fake.SyncCalls);
        Assert.Single(fake.RedeemCalls);
    }

    [Fact]
    public async Task UpdateAdminTicketRequestStatus_delegates_to_port()
    {
        var tid = Guid.NewGuid();
        var fake = new FakeTicketPort { Mutation = TicketMutationResult.Success() };
        var handler = new UpdateAdminTicketRequestStatusCommandHandler(fake);
        var r = await handler.Handle(new UpdateAdminTicketRequestStatusCommand(tid, "Issued"), CancellationToken.None);
        Assert.True(r.Ok);
        Assert.Single(fake.UpdateRequestStatusCalls);
        Assert.Equal(tid, fake.UpdateRequestStatusCalls[0].TicketId);
        Assert.Equal("Issued", fake.UpdateRequestStatusCalls[0].RequestStatus);
    }

    private sealed class FakeTicketPort : ITicketAdministrationPort
    {
        public List<(Guid? UserId, Guid? GameId, string? Status, string? RequestStatus, int Page, int PageSize)> ListCalls { get; } = [];
        public List<Guid> DetailCalls { get; } = [];
        public List<(Guid UserId, Guid GameId)> ReserveCalls { get; } = [];
        public List<Guid> PurchaseCalls { get; } = [];
        public List<Guid> SyncCalls { get; } = [];
        public List<Guid> RedeemCalls { get; } = [];
        public List<(Guid TicketId, string RequestStatus)> UpdateRequestStatusCalls { get; } = [];

        public TicketReserveResult ReserveResult { get; init; } = new(null, TicketMutationError.UserNotFound);
        public TicketMutationResult Mutation { get; init; } = TicketMutationResult.Fail(TicketMutationError.NotFound);

        public Task<AdminTicketListPageDto> ListTicketsAsync(
            Guid? userId,
            Guid? gameId,
            string? status,
            string? requestStatus,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            ListCalls.Add((userId, gameId, status, requestStatus, page, pageSize));
            return Task.FromResult(new AdminTicketListPageDto(0, []));
        }

        public Task<AdminTicketDetailDto?> GetTicketByIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
        {
            DetailCalls.Add(ticketId);
            return Task.FromResult<AdminTicketDetailDto?>(null);
        }

        public Task<TicketReserveResult> ReserveTicketAsync(Guid userId, Guid gameId, CancellationToken cancellationToken = default)
        {
            ReserveCalls.Add((userId, gameId));
            return Task.FromResult(ReserveResult);
        }

        public Task<TicketMutationResult> PurchaseTicketAsync(Guid ticketId, CancellationToken cancellationToken = default)
        {
            PurchaseCalls.Add(ticketId);
            return Task.FromResult(Mutation);
        }

        public Task<TicketMutationResult> SyncTicketAsync(Guid ticketId, CancellationToken cancellationToken = default)
        {
            SyncCalls.Add(ticketId);
            return Task.FromResult(Mutation);
        }

        public Task<TicketMutationResult> RedeemTicketAsync(Guid ticketId, CancellationToken cancellationToken = default)
        {
            RedeemCalls.Add(ticketId);
            return Task.FromResult(Mutation);
        }

        public Task<TicketMutationResult> UpdateTicketRequestStatusAsync(
            Guid ticketId,
            string requestStatus,
            CancellationToken cancellationToken = default)
        {
            UpdateRequestStatusCalls.Add((ticketId, requestStatus));
            return Task.FromResult(Mutation);
        }
    }
}
