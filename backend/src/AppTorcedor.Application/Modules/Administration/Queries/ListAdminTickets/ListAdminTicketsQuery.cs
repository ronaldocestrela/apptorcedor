using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListAdminTickets;

public sealed record ListAdminTicketsQuery(
    Guid? UserId,
    Guid? GameId,
    string? Status,
    int Page,
    int PageSize) : IRequest<AdminTicketListPageDto>;
