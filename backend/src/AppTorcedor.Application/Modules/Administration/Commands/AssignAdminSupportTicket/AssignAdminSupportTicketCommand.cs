using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.AssignAdminSupportTicket;

public sealed record AssignAdminSupportTicketCommand(
    Guid TicketId,
    Guid? AgentUserId,
    Guid ActorUserId) : IRequest<SupportTicketMutationResult>;
