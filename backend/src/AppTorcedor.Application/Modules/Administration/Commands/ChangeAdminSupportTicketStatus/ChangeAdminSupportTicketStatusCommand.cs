using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.ChangeAdminSupportTicketStatus;

public sealed record ChangeAdminSupportTicketStatusCommand(
    Guid TicketId,
    SupportTicketStatus NewStatus,
    string? Reason,
    Guid ActorUserId) : IRequest<SupportTicketMutationResult>;
