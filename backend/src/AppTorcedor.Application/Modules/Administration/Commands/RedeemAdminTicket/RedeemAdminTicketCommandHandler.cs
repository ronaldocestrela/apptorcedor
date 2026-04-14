using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.RedeemAdminTicket;

public sealed class RedeemAdminTicketCommandHandler(ITicketAdministrationPort tickets)
    : IRequestHandler<RedeemAdminTicketCommand, TicketMutationResult>
{
    public Task<TicketMutationResult> Handle(RedeemAdminTicketCommand request, CancellationToken cancellationToken) =>
        tickets.RedeemTicketAsync(request.TicketId, cancellationToken);
}
