using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.GetMySupportTicket;

public sealed class GetMySupportTicketQueryHandler(ISupportTorcedorPort port)
    : IRequestHandler<GetMySupportTicketQuery, TorcedorSupportTicketDetailDto?>
{
    public Task<TorcedorSupportTicketDetailDto?> Handle(GetMySupportTicketQuery request, CancellationToken cancellationToken) =>
        port.GetMyTicketAsync(request.TicketId, request.UserId, cancellationToken);
}
