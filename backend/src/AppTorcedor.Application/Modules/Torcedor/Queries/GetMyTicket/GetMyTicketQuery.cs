using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.GetMyTicket;

public sealed record GetMyTicketQuery(Guid UserId, Guid TicketId) : IRequest<TorcedorTicketDetailDto?>;
