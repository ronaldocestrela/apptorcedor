using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Commands.ReopenMySupportTicket;

public sealed record ReopenMySupportTicketCommand(Guid UserId, Guid TicketId) : IRequest<SupportTicketMutationResult>;
