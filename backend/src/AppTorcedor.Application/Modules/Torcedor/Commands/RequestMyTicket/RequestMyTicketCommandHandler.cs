using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Commands.RequestMyTicket;

public sealed class RequestMyTicketCommandHandler(ITicketTorcedorPort tickets)
    : IRequestHandler<RequestMyTicketCommand, TicketReserveResult>
{
    public Task<TicketReserveResult> Handle(RequestMyTicketCommand request, CancellationToken cancellationToken) =>
        tickets.RequestMyTicketAsync(request.UserId, request.GameId, cancellationToken);
}
