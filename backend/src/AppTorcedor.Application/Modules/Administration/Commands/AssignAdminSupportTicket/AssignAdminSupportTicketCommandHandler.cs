using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.AssignAdminSupportTicket;

public sealed class AssignAdminSupportTicketCommandHandler(ISupportAdministrationPort port)
    : IRequestHandler<AssignAdminSupportTicketCommand, SupportTicketMutationResult>
{
    public Task<SupportTicketMutationResult> Handle(AssignAdminSupportTicketCommand request, CancellationToken cancellationToken) =>
        port.AssignTicketAsync(request.TicketId, request.AgentUserId, request.ActorUserId, cancellationToken);
}
