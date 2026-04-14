using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Commands.ReopenMySupportTicket;

public sealed class ReopenMySupportTicketCommandHandler(ISupportTorcedorPort port)
    : IRequestHandler<ReopenMySupportTicketCommand, SupportTicketMutationResult>
{
    public Task<SupportTicketMutationResult> Handle(ReopenMySupportTicketCommand request, CancellationToken cancellationToken) =>
        port.ReopenMyTicketAsync(request.TicketId, request.UserId, cancellationToken);
}
