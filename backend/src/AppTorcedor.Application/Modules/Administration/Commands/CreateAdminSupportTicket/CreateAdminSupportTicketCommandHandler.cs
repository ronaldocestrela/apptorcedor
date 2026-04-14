using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.CreateAdminSupportTicket;

public sealed class CreateAdminSupportTicketCommandHandler(ISupportAdministrationPort port)
    : IRequestHandler<CreateAdminSupportTicketCommand, SupportTicketCreateResult>
{
    public Task<SupportTicketCreateResult> Handle(CreateAdminSupportTicketCommand request, CancellationToken cancellationToken) =>
        port.CreateTicketAsync(request.Dto, request.ActorUserId, cancellationToken);
}
