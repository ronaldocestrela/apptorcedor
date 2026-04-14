using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.ReplyAdminSupportTicket;

public sealed class ReplyAdminSupportTicketCommandHandler(ISupportAdministrationPort port)
    : IRequestHandler<ReplyAdminSupportTicketCommand, SupportTicketMutationResult>
{
    public Task<SupportTicketMutationResult> Handle(ReplyAdminSupportTicketCommand request, CancellationToken cancellationToken) =>
        port.ReplyTicketAsync(request.TicketId, request.Body, request.IsInternal, request.ActorUserId, cancellationToken);
}
