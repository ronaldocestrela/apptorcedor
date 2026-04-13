using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.ReserveAdminTicket;

public sealed record ReserveAdminTicketCommand(Guid UserId, Guid GameId) : IRequest<TicketReserveResult>;
