using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.RedeemAdminTicket;

public sealed record RedeemAdminTicketCommand(Guid TicketId) : IRequest<TicketMutationResult>;
