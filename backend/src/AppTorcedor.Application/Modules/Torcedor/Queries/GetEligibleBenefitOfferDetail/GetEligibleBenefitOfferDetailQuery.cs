using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.GetEligibleBenefitOfferDetail;

public sealed record GetEligibleBenefitOfferDetailQuery(Guid UserId, Guid OfferId)
    : IRequest<TorcedorEligibleBenefitOfferDetailDto?>;
