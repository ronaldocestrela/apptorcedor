using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.GetTorcedorMonthlyLoyaltyRanking;

public sealed class GetTorcedorMonthlyLoyaltyRankingQueryHandler(ILoyaltyTorcedorReadPort loyalty)
    : IRequestHandler<GetTorcedorMonthlyLoyaltyRankingQuery, LoyaltyTorcedorRankingPageDto>
{
    public Task<LoyaltyTorcedorRankingPageDto> Handle(
        GetTorcedorMonthlyLoyaltyRankingQuery request,
        CancellationToken cancellationToken) =>
        loyalty.GetMonthlyRankingAsync(
            request.UserId,
            request.Year,
            request.Month,
            request.Page,
            request.PageSize,
            cancellationToken);
}
