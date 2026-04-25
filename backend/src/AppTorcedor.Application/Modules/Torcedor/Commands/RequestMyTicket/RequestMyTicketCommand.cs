using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Commands.RequestMyTicket;

public sealed record RequestMyTicketCommand(Guid UserId, Guid GameId) : IRequest<TicketReserveResult>;
