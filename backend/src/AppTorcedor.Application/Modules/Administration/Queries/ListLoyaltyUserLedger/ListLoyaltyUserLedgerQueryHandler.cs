using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListLoyaltyUserLedger;

public sealed class ListLoyaltyUserLedgerQueryHandler(ILoyaltyAdministrationPort loyalty)
    : IRequestHandler<ListLoyaltyUserLedgerQuery, LoyaltyLedgerPageDto>
{
    public Task<LoyaltyLedgerPageDto> Handle(ListLoyaltyUserLedgerQuery request, CancellationToken cancellationToken) =>
        loyalty.ListUserLedgerAsync(request.UserId, request.Page, request.PageSize, cancellationToken);
}
