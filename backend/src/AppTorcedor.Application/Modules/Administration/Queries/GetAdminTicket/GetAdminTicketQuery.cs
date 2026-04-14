using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetAdminTicket;

public sealed record GetAdminTicketQuery(Guid TicketId) : IRequest<AdminTicketDetailDto?>;
