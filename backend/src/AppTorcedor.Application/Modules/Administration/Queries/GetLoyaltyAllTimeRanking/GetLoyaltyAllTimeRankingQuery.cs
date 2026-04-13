using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetLoyaltyAllTimeRanking;

public sealed record GetLoyaltyAllTimeRankingQuery(int Page, int PageSize) : IRequest<LoyaltyRankingPageDto>;
