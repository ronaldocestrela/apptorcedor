using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.CreateBenefitOffer;

public sealed record CreateBenefitOfferCommand(BenefitOfferWriteDto Dto) : IRequest<BenefitCreateResult>;
