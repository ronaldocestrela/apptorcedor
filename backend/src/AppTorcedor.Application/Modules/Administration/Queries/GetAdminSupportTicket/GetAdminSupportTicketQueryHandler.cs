using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetAdminSupportTicket;

public sealed class GetAdminSupportTicketQueryHandler(ISupportAdministrationPort port)
    : IRequestHandler<GetAdminSupportTicketQuery, AdminSupportTicketDetailDto?>
{
    public Task<AdminSupportTicketDetailDto?> Handle(GetAdminSupportTicketQuery request, CancellationToken cancellationToken) =>
        port.GetTicketByIdAsync(request.TicketId, cancellationToken);
}
