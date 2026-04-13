using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.ListEligibleBenefitOffers;

public sealed record ListEligibleBenefitOffersQuery(Guid UserId, int Page, int PageSize)
    : IRequest<TorcedorEligibleBenefitOffersPageDto>;
