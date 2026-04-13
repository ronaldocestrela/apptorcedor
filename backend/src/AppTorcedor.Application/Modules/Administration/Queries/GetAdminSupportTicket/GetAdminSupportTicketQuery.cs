using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetAdminSupportTicket;

public sealed record GetAdminSupportTicketQuery(Guid TicketId) : IRequest<AdminSupportTicketDetailDto?>;
