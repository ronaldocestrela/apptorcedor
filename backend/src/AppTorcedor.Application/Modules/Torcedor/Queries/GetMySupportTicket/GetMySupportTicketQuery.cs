using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.GetMySupportTicket;

public sealed record GetMySupportTicketQuery(Guid UserId, Guid TicketId) : IRequest<TorcedorSupportTicketDetailDto?>;
