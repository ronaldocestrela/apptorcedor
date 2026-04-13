using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.ListMySupportTickets;

public sealed class ListMySupportTicketsQueryHandler(ISupportTorcedorPort port)
    : IRequestHandler<ListMySupportTicketsQuery, TorcedorSupportTicketListPageDto>
{
    public Task<TorcedorSupportTicketListPageDto> Handle(ListMySupportTicketsQuery request, CancellationToken cancellationToken) =>
        port.ListMyTicketsAsync(request.UserId, request.Status, request.Page, request.PageSize, cancellationToken);
}
