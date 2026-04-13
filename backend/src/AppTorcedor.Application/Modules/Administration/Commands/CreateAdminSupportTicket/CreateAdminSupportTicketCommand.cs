using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.CreateAdminSupportTicket;

public sealed record CreateAdminSupportTicketCommand(AdminSupportTicketCreateDto Dto, Guid ActorUserId)
    : IRequest<SupportTicketCreateResult>;
