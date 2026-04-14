using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Commands.RedeemMyTicket;

public sealed class RedeemMyTicketCommandHandler(ITicketTorcedorPort tickets)
    : IRequestHandler<RedeemMyTicketCommand, TicketMutationResult>
{
    public Task<TicketMutationResult> Handle(RedeemMyTicketCommand request, CancellationToken cancellationToken) =>
        tickets.RedeemMyTicketAsync(request.UserId, request.TicketId, cancellationToken);
}
