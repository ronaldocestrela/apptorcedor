using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetBenefitOffer;

public sealed class GetBenefitOfferQueryHandler(IBenefitsAdministrationPort benefits)
    : IRequestHandler<GetBenefitOfferQuery, BenefitOfferDetailDto?>
{
    public Task<BenefitOfferDetailDto?> Handle(GetBenefitOfferQuery request, CancellationToken cancellationToken) =>
        benefits.GetOfferByIdAsync(request.OfferId, cancellationToken);
}
