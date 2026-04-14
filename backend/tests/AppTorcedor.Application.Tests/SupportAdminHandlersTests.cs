using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Commands.AssignAdminSupportTicket;
using AppTorcedor.Application.Modules.Administration.Commands.ChangeAdminSupportTicketStatus;
using AppTorcedor.Application.Modules.Administration.Commands.CreateAdminSupportTicket;
using AppTorcedor.Application.Modules.Administration.Commands.ReplyAdminSupportTicket;
using AppTorcedor.Application.Modules.Administration.Queries.GetAdminSupportTicket;
using AppTorcedor.Application.Modules.Administration.Queries.ListAdminSupportTickets;

namespace AppTorcedor.Application.Tests;

public sealed class SupportAdminHandlersTests
{
    [Fact]
    public async Task ListAdminSupportTickets_delegates_to_port()
    {
        var fake = new FakeSupportPort();
        var handler = new ListAdminSupportTicketsQueryHandler(fake);
        var page = await handler.Handle(
            new ListAdminSupportTicketsQuery("q", SupportTicketStatus.Open, null, true, false, 1, 10),
            CancellationToken.None);
        Assert.Equal(0, page.TotalCount);
        Assert.Single(fake.ListCalls);
        Assert.Equal("q", fake.ListCalls[0].Queue);
        Assert.Equal(SupportTicketStatus.Open, fake.ListCalls[0].Status);
        Assert.True(fake.ListCalls[0].UnassignedOnly);
    }

    [Fact]
    public async Task GetAdminSupportTicket_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var fake = new FakeSupportPort();
        var handler = new GetAdminSupportTicketQueryHandler(fake);
        await handler.Handle(new GetAdminSupportTicketQuery(id), CancellationToken.None);
        Assert.Single(fake.GetCalls);
        Assert.Equal(id, fake.GetCalls[0]);
    }

    [Fact]
    public async Task CreateAdminSupportTicket_delegates_to_port()
    {
        var dto = new AdminSupportTicketCreateDto(Guid.NewGuid(), "Geral", "Assunto", SupportTicketPriority.Normal, null);
        var actor = Guid.NewGuid();
        var fake = new FakeSupportPort { CreateResult = new SupportTicketCreateResult(Guid.NewGuid(), null) };
        var handler = new CreateAdminSupportTicketCommandHandler(fake);
        var r = await handler.Handle(new CreateAdminSupportTicketCommand(dto, actor), CancellationToken.None);
        Assert.Null(r.Error);
        Assert.NotNull(r.TicketId);
        Assert.Single(fake.CreateCalls);
    }

    [Fact]
    public async Task ReplyAdminSupportTicket_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var fake = new FakeSupportPort { Mutation = new SupportTicketMutationResult(true, null) };
        var handler = new ReplyAdminSupportTicketCommandHandler(fake);
        var r = await handler.Handle(new ReplyAdminSupportTicketCommand(id, "Olá", false, actor), CancellationToken.None);
        Assert.True(r.Ok);
        Assert.Single(fake.ReplyCalls);
    }

    [Fact]
    public async Task AssignAdminSupportTicket_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var agent = Guid.NewGuid();
        var fake = new FakeSupportPort { Mutation = new SupportTicketMutationResult(true, null) };
        var handler = new AssignAdminSupportTicketCommandHandler(fake);
        var r = await handler.Handle(new AssignAdminSupportTicketCommand(id, agent, actor), CancellationToken.None);
        Assert.True(r.Ok);
        Assert.Single(fake.AssignCalls);
    }

    [Fact]
    public async Task ChangeAdminSupportTicketStatus_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var fake = new FakeSupportPort { Mutation = new SupportTicketMutationResult(true, null) };
        var handler = new ChangeAdminSupportTicketStatusCommandHandler(fake);
        var r = await handler.Handle(
            new ChangeAdminSupportTicketStatusCommand(id, SupportTicketStatus.InProgress, "ok", actor),
            CancellationToken.None);
        Assert.True(r.Ok);
        Assert.Single(fake.StatusCalls);
    }

    private sealed class FakeSupportPort : ISupportAdministrationPort
    {
        public List<(string? Queue, SupportTicketStatus? Status, Guid? AssignedUserId, bool? UnassignedOnly, bool? SlaBreachedOnly, int Page, int PageSize)> ListCalls { get; } = [];
        public List<Guid> GetCalls { get; } = [];
        public List<(AdminSupportTicketCreateDto Dto, Guid Actor)> CreateCalls { get; } = [];
        public List<(Guid Id, string Body, bool Internal, Guid Actor)> ReplyCalls { get; } = [];
        public List<(Guid Id, Guid? Agent, Guid Actor)> AssignCalls { get; } = [];
        public List<(Guid Id, SupportTicketStatus Status, string? Reason, Guid Actor)> StatusCalls { get; } = [];

        public SupportTicketCreateResult CreateResult { get; init; } = new(null, SupportTicketMutationError.Validation);
        public SupportTicketMutationResult Mutation { get; init; } = new(false, SupportTicketMutationError.NotFound);

        public Task<AdminSupportTicketListPageDto> ListTicketsAsync(
            string? queue,
            SupportTicketStatus? status,
            Guid? assignedUserId,
            bool? unassignedOnly,
            bool? slaBreachedOnly,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            ListCalls.Add((queue, status, assignedUserId, unassignedOnly, slaBreachedOnly, page, pageSize));
            return Task.FromResult(new AdminSupportTicketListPageDto(0, []));
        }

        public Task<AdminSupportTicketDetailDto?> GetTicketByIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
        {
            GetCalls.Add(ticketId);
            return Task.FromResult<AdminSupportTicketDetailDto?>(null);
        }

        public Task<SupportTicketCreateResult> CreateTicketAsync(
            AdminSupportTicketCreateDto dto,
            Guid actorUserId,
            CancellationToken cancellationToken = default)
        {
            CreateCalls.Add((dto, actorUserId));
            return Task.FromResult(CreateResult);
        }

        public Task<SupportTicketMutationResult> ReplyTicketAsync(
            Guid ticketId,
            string body,
            bool isInternal,
            Guid actorUserId,
            CancellationToken cancellationToken = default)
        {
            ReplyCalls.Add((ticketId, body, isInternal, actorUserId));
            return Task.FromResult(Mutation);
        }

        public Task<SupportTicketMutationResult> AssignTicketAsync(
            Guid ticketId,
            Guid? agentUserId,
            Guid actorUserId,
            CancellationToken cancellationToken = default)
        {
            AssignCalls.Add((ticketId, agentUserId, actorUserId));
            return Task.FromResult(Mutation);
        }

        public Task<SupportTicketMutationResult> ChangeStatusAsync(
            Guid ticketId,
            SupportTicketStatus newStatus,
            string? reason,
            Guid actorUserId,
            CancellationToken cancellationToken = default)
        {
            StatusCalls.Add((ticketId, newStatus, reason, actorUserId));
            return Task.FromResult(Mutation);
        }

        public Task<SupportAttachmentDownloadDto?> GetSupportAttachmentDownloadAsync(
            Guid ticketId,
            Guid attachmentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<SupportAttachmentDownloadDto?>(null);
    }
}
