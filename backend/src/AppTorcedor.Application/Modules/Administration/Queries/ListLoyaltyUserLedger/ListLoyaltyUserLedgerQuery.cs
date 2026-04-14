using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListLoyaltyUserLedger;

public sealed record ListLoyaltyUserLedgerQuery(Guid UserId, int Page, int PageSize) : IRequest<LoyaltyLedgerPageDto>;
