using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.GetTorcedorAllTimeLoyaltyRanking;

public sealed class GetTorcedorAllTimeLoyaltyRankingQueryHandler(ILoyaltyTorcedorReadPort loyalty)
    : IRequestHandler<GetTorcedorAllTimeLoyaltyRankingQuery, LoyaltyTorcedorRankingPageDto>
{
    public Task<LoyaltyTorcedorRankingPageDto> Handle(
        GetTorcedorAllTimeLoyaltyRankingQuery request,
        CancellationToken cancellationToken) =>
        loyalty.GetAllTimeRankingAsync(request.UserId, request.Page, request.PageSize, cancellationToken);
}
