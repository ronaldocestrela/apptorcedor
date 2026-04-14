using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListAdminSupportTickets;

public sealed class ListAdminSupportTicketsQueryHandler(ISupportAdministrationPort port)
    : IRequestHandler<ListAdminSupportTicketsQuery, AdminSupportTicketListPageDto>
{
    public Task<AdminSupportTicketListPageDto> Handle(ListAdminSupportTicketsQuery request, CancellationToken cancellationToken) =>
        port.ListTicketsAsync(
            request.Queue,
            request.Status,
            request.AssignedUserId,
            request.UnassignedOnly,
            request.SlaBreachedOnly,
            request.Page,
            request.PageSize,
            cancellationToken);
}
