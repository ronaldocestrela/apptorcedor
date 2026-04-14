using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListAdminSupportTickets;

public sealed record ListAdminSupportTicketsQuery(
    string? Queue,
    SupportTicketStatus? Status,
    Guid? AssignedUserId,
    bool? UnassignedOnly,
    bool? SlaBreachedOnly,
    int Page,
    int PageSize) : IRequest<AdminSupportTicketListPageDto>;
