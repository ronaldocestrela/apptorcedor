using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.UploadBenefitOfferBanner;

public sealed record UploadBenefitOfferBannerCommand(Guid OfferId, Stream Content, string FileName, string ContentType)
    : IRequest<BenefitBannerUploadResult>;
