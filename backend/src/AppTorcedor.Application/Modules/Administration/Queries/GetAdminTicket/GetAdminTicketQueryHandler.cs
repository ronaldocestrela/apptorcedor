using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetAdminTicket;

public sealed class GetAdminTicketQueryHandler(ITicketAdministrationPort tickets)
    : IRequestHandler<GetAdminTicketQuery, AdminTicketDetailDto?>
{
    public Task<AdminTicketDetailDto?> Handle(GetAdminTicketQuery request, CancellationToken cancellationToken) =>
        tickets.GetTicketByIdAsync(request.TicketId, cancellationToken);
}
