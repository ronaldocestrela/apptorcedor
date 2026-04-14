using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.ChangeAdminSupportTicketStatus;

public sealed class ChangeAdminSupportTicketStatusCommandHandler(ISupportAdministrationPort port)
    : IRequestHandler<ChangeAdminSupportTicketStatusCommand, SupportTicketMutationResult>
{
    public Task<SupportTicketMutationResult> Handle(ChangeAdminSupportTicketStatusCommand request, CancellationToken cancellationToken) =>
        port.ChangeStatusAsync(request.TicketId, request.NewStatus, request.Reason, request.ActorUserId, cancellationToken);
}
