using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.RemoveBenefitOfferBanner;

public sealed class RemoveBenefitOfferBannerCommandHandler(IBenefitsAdministrationPort benefits)
    : IRequestHandler<RemoveBenefitOfferBannerCommand, BenefitMutationResult>
{
    public Task<BenefitMutationResult> Handle(RemoveBenefitOfferBannerCommand request, CancellationToken cancellationToken) =>
        benefits.RemoveOfferBannerAsync(request.OfferId, cancellationToken);
}
