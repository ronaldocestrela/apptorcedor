using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.ReplyAdminSupportTicket;

public sealed record ReplyAdminSupportTicketCommand(
    Guid TicketId,
    string Body,
    bool IsInternal,
    Guid ActorUserId) : IRequest<SupportTicketMutationResult>;
