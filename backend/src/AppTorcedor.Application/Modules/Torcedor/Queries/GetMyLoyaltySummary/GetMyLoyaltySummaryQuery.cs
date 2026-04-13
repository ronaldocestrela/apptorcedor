using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.GetMyLoyaltySummary;

public sealed record GetMyLoyaltySummaryQuery(Guid UserId) : IRequest<LoyaltyTorcedorSummaryDto>;
