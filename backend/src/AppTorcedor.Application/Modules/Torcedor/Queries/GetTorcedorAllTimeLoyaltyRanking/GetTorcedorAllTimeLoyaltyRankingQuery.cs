using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.GetTorcedorAllTimeLoyaltyRanking;

public sealed record GetTorcedorAllTimeLoyaltyRankingQuery(Guid UserId, int Page, int PageSize)
    : IRequest<LoyaltyTorcedorRankingPageDto>;
