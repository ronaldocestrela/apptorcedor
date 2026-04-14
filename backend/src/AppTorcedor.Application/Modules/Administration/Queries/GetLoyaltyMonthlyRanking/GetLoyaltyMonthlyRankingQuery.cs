using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetLoyaltyMonthlyRanking;

public sealed record GetLoyaltyMonthlyRankingQuery(int Year, int Month, int Page, int PageSize)
    : IRequest<LoyaltyRankingPageDto>;
