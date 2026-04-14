using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.GetTorcedorMonthlyLoyaltyRanking;

public sealed record GetTorcedorMonthlyLoyaltyRankingQuery(
    Guid UserId,
    int Year,
    int Month,
    int Page,
    int PageSize) : IRequest<LoyaltyTorcedorRankingPageDto>;
