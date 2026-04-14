using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.ListMySupportTickets;

public sealed record ListMySupportTicketsQuery(
    Guid UserId,
    SupportTicketStatus? Status,
    int Page,
    int PageSize) : IRequest<TorcedorSupportTicketListPageDto>;
