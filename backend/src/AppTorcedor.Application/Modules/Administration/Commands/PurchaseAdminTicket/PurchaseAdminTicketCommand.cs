using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.PurchaseAdminTicket;

public sealed record PurchaseAdminTicketCommand(Guid TicketId) : IRequest<TicketMutationResult>;
