using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListAdminDigitalCards;

public sealed class ListAdminDigitalCardsQueryHandler(IDigitalCardAdministrationPort port)
    : IRequestHandler<ListAdminDigitalCardsQuery, AdminDigitalCardListPageDto>
{
    public Task<AdminDigitalCardListPageDto> Handle(ListAdminDigitalCardsQuery request, CancellationToken cancellationToken) =>
        port.ListDigitalCardsAsync(
            request.UserId,
            request.MembershipId,
            request.Status,
            request.Page,
            request.PageSize,
            cancellationToken);
}
