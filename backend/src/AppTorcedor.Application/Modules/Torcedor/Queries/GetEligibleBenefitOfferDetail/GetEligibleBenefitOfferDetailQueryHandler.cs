using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.GetEligibleBenefitOfferDetail;

public sealed class GetEligibleBenefitOfferDetailQueryHandler(ITorcedorBenefitsReadPort port)
    : IRequestHandler<GetEligibleBenefitOfferDetailQuery, TorcedorEligibleBenefitOfferDetailDto?>
{
    public Task<TorcedorEligibleBenefitOfferDetailDto?> Handle(
        GetEligibleBenefitOfferDetailQuery request,
        CancellationToken cancellationToken) =>
        port.GetEligibleOfferDetailAsync(request.UserId, request.OfferId, cancellationToken);
}
