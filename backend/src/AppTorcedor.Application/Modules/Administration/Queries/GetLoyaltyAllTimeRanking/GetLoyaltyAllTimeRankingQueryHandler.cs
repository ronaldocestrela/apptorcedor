using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetLoyaltyAllTimeRanking;

public sealed class GetLoyaltyAllTimeRankingQueryHandler(ILoyaltyAdministrationPort loyalty)
    : IRequestHandler<GetLoyaltyAllTimeRankingQuery, LoyaltyRankingPageDto>
{
    public Task<LoyaltyRankingPageDto> Handle(GetLoyaltyAllTimeRankingQuery request, CancellationToken cancellationToken) =>
        loyalty.GetAllTimeRankingAsync(request.Page, request.PageSize, cancellationToken);
}
