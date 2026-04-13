using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Torcedor.Commands.CancelMySupportTicket;
using AppTorcedor.Application.Modules.Torcedor.Commands.CreateMySupportTicket;
using AppTorcedor.Application.Modules.Torcedor.Commands.ReopenMySupportTicket;
using AppTorcedor.Application.Modules.Torcedor.Commands.ReplyMySupportTicket;
using AppTorcedor.Application.Modules.Torcedor.Queries.GetMySupportTicket;
using AppTorcedor.Application.Modules.Torcedor.Queries.ListMySupportTickets;
using Xunit;

namespace AppTorcedor.Application.Tests;

public sealed class SupportTorcedorHandlersTests
{
    private static readonly Guid User = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task ListMySupportTickets_delegates_to_port()
    {
        var fake = new FakeSupportTorcedorPort();
        var handler = new ListMySupportTicketsQueryHandler(fake);
        await handler.Handle(new ListMySupportTicketsQuery(User, SupportTicketStatus.Open, 1, 10), CancellationToken.None);
        Assert.Single(fake.ListCalls);
        Assert.Equal(User, fake.ListCalls[0].UserId);
        Assert.Equal(SupportTicketStatus.Open, fake.ListCalls[0].Status);
    }

    [Fact]
    public async Task CreateMySupportTicket_delegates_to_port()
    {
        var fake = new FakeSupportTorcedorPort
        {
            CreateResult = new SupportTicketCreateResult(Guid.NewGuid(), null),
        };
        var handler = new CreateMySupportTicketCommandHandler(fake);
        var att = new[] { new SupportTorcedorAttachmentInput([1], "a.png", "image/png") };
        var r = await handler.Handle(
            new CreateMySupportTicketCommand(User, "Geral", "Assunto", SupportTicketPriority.Normal, "Olá", att),
            CancellationToken.None);
        Assert.NotNull(r.TicketId);
        Assert.Null(r.Error);
        Assert.Single(fake.CreateCalls);
    }

    [Fact]
    public async Task GetMySupportTicket_delegates_to_port()
    {
        var fake = new FakeSupportTorcedorPort();
        var handler = new GetMySupportTicketQueryHandler(fake);
        var tid = Guid.NewGuid();
        await handler.Handle(new GetMySupportTicketQuery(User, tid), CancellationToken.None);
        Assert.Equal(tid, fake.GetCalls[0].TicketId);
    }

    [Fact]
    public async Task ReplyMySupportTicket_delegates_to_port()
    {
        var fake = new FakeSupportTorcedorPort { Mutation = new SupportTicketMutationResult(true, null) };
        var handler = new ReplyMySupportTicketCommandHandler(fake);
        var tid = Guid.NewGuid();
        var r = await handler.Handle(
            new ReplyMySupportTicketCommand(User, tid, "Oi", []),
            CancellationToken.None);
        Assert.True(r.Ok);
 }

    [Fact]
    public async Task CancelMySupportTicket_delegates_to_port()
    {
        var fake = new FakeSupportTorcedorPort { Mutation = new SupportTicketMutationResult(true, null) };
        var handler = new CancelMySupportTicketCommandHandler(fake);
        var tid = Guid.NewGuid();
        var r = await handler.Handle(new CancelMySupportTicketCommand(User, tid, "motivo"), CancellationToken.None);
        Assert.True(r.Ok);
    }

    [Fact]
    public async Task ReopenMySupportTicket_delegates_to_port()
    {
        var fake = new FakeSupportTorcedorPort { Mutation = new SupportTicketMutationResult(true, null) };
        var handler = new ReopenMySupportTicketCommandHandler(fake);
        var tid = Guid.NewGuid();
        var r = await handler.Handle(new ReopenMySupportTicketCommand(User, tid), CancellationToken.None);
        Assert.True(r.Ok);
    }

    private sealed class FakeSupportTorcedorPort : ISupportTorcedorPort
    {
        public List<(Guid UserId, SupportTicketStatus? Status, int Page, int PageSize)> ListCalls { get; } = [];
        public List<(Guid TicketId, Guid UserId)> GetCalls { get; } = [];
        public List<(Guid UserId, string Queue, string Subject, SupportTicketPriority Priority, string? Message, IReadOnlyList<SupportTorcedorAttachmentInput> Att)> CreateCalls { get; } = [];

        public SupportTicketCreateResult CreateResult { get; init; } = new(null, SupportTicketMutationError.Validation);
        public SupportTicketMutationResult Mutation { get; init; } = new(false, SupportTicketMutationError.NotFound);

        public Task<TorcedorSupportTicketListPageDto> ListMyTicketsAsync(
            Guid requesterUserId,
            SupportTicketStatus? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            ListCalls.Add((requesterUserId, status, page, pageSize));
            return Task.FromResult(new TorcedorSupportTicketListPageDto(0, []));
        }

        public Task<TorcedorSupportTicketDetailDto?> GetMyTicketAsync(
            Guid ticketId,
            Guid requesterUserId,
            CancellationToken cancellationToken = default)
        {
            GetCalls.Add((ticketId, requesterUserId));
            return Task.FromResult<TorcedorSupportTicketDetailDto?>(null);
        }

        public Task<SupportTicketCreateResult> CreateMyTicketAsync(
            Guid requesterUserId,
            string queue,
            string subject,
            SupportTicketPriority priority,
            string? initialMessage,
            IReadOnlyList<SupportTorcedorAttachmentInput> attachments,
            CancellationToken cancellationToken = default)
        {
            CreateCalls.Add((requesterUserId, queue, subject, priority, initialMessage, attachments));
            return Task.FromResult(CreateResult);
        }

        public Task<SupportTicketMutationResult> ReplyMyTicketAsync(
            Guid ticketId,
            Guid requesterUserId,
            string body,
            IReadOnlyList<SupportTorcedorAttachmentInput> attachments,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Mutation);

        public Task<SupportTicketMutationResult> CancelMyTicketAsync(
            Guid ticketId,
            Guid requesterUserId,
            string? reason,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Mutation);

        public Task<SupportTicketMutationResult> ReopenMyTicketAsync(
            Guid ticketId,
            Guid requesterUserId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Mutation);
    }
}
