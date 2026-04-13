using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Commands.CancelMySupportTicket;

public sealed class CancelMySupportTicketCommandHandler(ISupportTorcedorPort port)
    : IRequestHandler<CancelMySupportTicketCommand, SupportTicketMutationResult>
{
    public Task<SupportTicketMutationResult> Handle(CancelMySupportTicketCommand request, CancellationToken cancellationToken) =>
        port.CancelMyTicketAsync(request.TicketId, request.UserId, request.Reason, cancellationToken);
}
