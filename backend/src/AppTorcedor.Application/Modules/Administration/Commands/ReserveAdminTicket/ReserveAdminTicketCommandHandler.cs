using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.ReserveAdminTicket;

public sealed class ReserveAdminTicketCommandHandler(ITicketAdministrationPort tickets)
    : IRequestHandler<ReserveAdminTicketCommand, TicketReserveResult>
{
    public Task<TicketReserveResult> Handle(ReserveAdminTicketCommand request, CancellationToken cancellationToken) =>
        tickets.ReserveTicketAsync(request.UserId, request.GameId, cancellationToken);
}
