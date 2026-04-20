using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.RemoveBenefitOfferBanner;

public sealed record RemoveBenefitOfferBannerCommand(Guid OfferId) : IRequest<BenefitMutationResult>;
