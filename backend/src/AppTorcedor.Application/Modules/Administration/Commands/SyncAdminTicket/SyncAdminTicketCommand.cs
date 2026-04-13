using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.SyncAdminTicket;

public sealed record SyncAdminTicketCommand(Guid TicketId) : IRequest<TicketMutationResult>;
