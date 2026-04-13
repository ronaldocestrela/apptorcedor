using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.UpdateBenefitOffer;

public sealed record UpdateBenefitOfferCommand(Guid OfferId, BenefitOfferWriteDto Dto) : IRequest<BenefitMutationResult>;
