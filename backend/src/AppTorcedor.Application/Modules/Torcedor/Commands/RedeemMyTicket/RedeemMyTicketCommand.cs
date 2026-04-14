using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Commands.RedeemMyTicket;

public sealed record RedeemMyTicketCommand(Guid UserId, Guid TicketId) : IRequest<TicketMutationResult>;
