using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.UpdateAdminTicketRequestStatus;

public sealed class UpdateAdminTicketRequestStatusCommandHandler(ITicketAdministrationPort tickets)
    : IRequestHandler<UpdateAdminTicketRequestStatusCommand, TicketMutationResult>
{
    public Task<TicketMutationResult> Handle(
        UpdateAdminTicketRequestStatusCommand request,
        CancellationToken cancellationToken) =>
        tickets.UpdateTicketRequestStatusAsync(request.TicketId, request.RequestStatus, cancellationToken);
}
