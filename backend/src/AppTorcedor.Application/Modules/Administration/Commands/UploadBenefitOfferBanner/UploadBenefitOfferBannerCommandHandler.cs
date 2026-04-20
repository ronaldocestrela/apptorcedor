using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.UploadBenefitOfferBanner;

public sealed class UploadBenefitOfferBannerCommandHandler(IBenefitsAdministrationPort benefits)
    : IRequestHandler<UploadBenefitOfferBannerCommand, BenefitBannerUploadResult>
{
    public Task<BenefitBannerUploadResult> Handle(UploadBenefitOfferBannerCommand request, CancellationToken cancellationToken) =>
        benefits.UploadOfferBannerAsync(request.OfferId, request.Content, request.FileName, request.ContentType, cancellationToken);
}
