using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.PurchaseAdminTicket;

public sealed class PurchaseAdminTicketCommandHandler(ITicketAdministrationPort tickets)
    : IRequestHandler<PurchaseAdminTicketCommand, TicketMutationResult>
{
    public Task<TicketMutationResult> Handle(PurchaseAdminTicketCommand request, CancellationToken cancellationToken) =>
        tickets.PurchaseTicketAsync(request.TicketId, cancellationToken);
}
