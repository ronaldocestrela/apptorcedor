using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Commands.CancelMySupportTicket;

public sealed record CancelMySupportTicketCommand(Guid UserId, Guid TicketId, string? Reason)
    : IRequest<SupportTicketMutationResult>;
