using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.UpdateAdminTicketRequestStatus;

public sealed record UpdateAdminTicketRequestStatusCommand(
    Guid TicketId,
    string RequestStatus) : IRequest<TicketMutationResult>;
