using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.ListMyTickets;

public sealed class ListMyTicketsQueryHandler(ITicketTorcedorPort tickets)
    : IRequestHandler<ListMyTicketsQuery, TorcedorTicketListPageDto>
{
    public Task<TorcedorTicketListPageDto> Handle(ListMyTicketsQuery request, CancellationToken cancellationToken) =>
        tickets.ListMyTicketsAsync(
            request.UserId,
            request.GameId,
            request.Status,
            request.Page,
            request.PageSize,
            cancellationToken);
}
