using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Commands.ReplyMySupportTicket;

public sealed class ReplyMySupportTicketCommandHandler(ISupportTorcedorPort port)
    : IRequestHandler<ReplyMySupportTicketCommand, SupportTicketMutationResult>
{
    public Task<SupportTicketMutationResult> Handle(ReplyMySupportTicketCommand request, CancellationToken cancellationToken) =>
        port.ReplyMyTicketAsync(request.TicketId, request.UserId, request.Body, request.Attachments, cancellationToken);
}
