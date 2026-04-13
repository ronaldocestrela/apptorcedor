using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetLoyaltyMonthlyRanking;

public sealed class GetLoyaltyMonthlyRankingQueryHandler(ILoyaltyAdministrationPort loyalty)
    : IRequestHandler<GetLoyaltyMonthlyRankingQuery, LoyaltyRankingPageDto>
{
    public Task<LoyaltyRankingPageDto> Handle(GetLoyaltyMonthlyRankingQuery request, CancellationToken cancellationToken) =>
        loyalty.GetMonthlyRankingAsync(request.Year, request.Month, request.Page, request.PageSize, cancellationToken);
}
