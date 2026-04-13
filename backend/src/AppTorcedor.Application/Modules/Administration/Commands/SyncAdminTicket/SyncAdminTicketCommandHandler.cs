using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.SyncAdminTicket;

public sealed class SyncAdminTicketCommandHandler(ITicketAdministrationPort tickets)
    : IRequestHandler<SyncAdminTicketCommand, TicketMutationResult>
{
    public Task<TicketMutationResult> Handle(SyncAdminTicketCommand request, CancellationToken cancellationToken) =>
        tickets.SyncTicketAsync(request.TicketId, cancellationToken);
}
