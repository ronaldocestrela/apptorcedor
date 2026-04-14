using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.GetMyTicket;

public sealed class GetMyTicketQueryHandler(ITicketTorcedorPort tickets)
    : IRequestHandler<GetMyTicketQuery, TorcedorTicketDetailDto?>
{
    public Task<TorcedorTicketDetailDto?> Handle(GetMyTicketQuery request, CancellationToken cancellationToken) =>
        tickets.GetMyTicketAsync(request.UserId, request.TicketId, cancellationToken);
}
