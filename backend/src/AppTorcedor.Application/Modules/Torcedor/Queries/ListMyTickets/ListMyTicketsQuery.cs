using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.ListMyTickets;

public sealed record ListMyTicketsQuery(
    Guid UserId,
    Guid? GameId,
    string? Status,
    int Page,
    int PageSize) : IRequest<TorcedorTicketListPageDto>;
