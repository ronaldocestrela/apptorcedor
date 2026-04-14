using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListAdminTickets;

public sealed class ListAdminTicketsQueryHandler(ITicketAdministrationPort tickets)
    : IRequestHandler<ListAdminTicketsQuery, AdminTicketListPageDto>
{
    public Task<AdminTicketListPageDto> Handle(ListAdminTicketsQuery request, CancellationToken cancellationToken) =>
        tickets.ListTicketsAsync(
            request.UserId,
            request.GameId,
            request.Status,
            request.Page,
            request.PageSize,
            cancellationToken);
}
