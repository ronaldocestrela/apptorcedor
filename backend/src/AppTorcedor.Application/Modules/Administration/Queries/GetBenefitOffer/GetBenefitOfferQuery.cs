using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetBenefitOffer;

public sealed record GetBenefitOfferQuery(Guid OfferId) : IRequest<BenefitOfferDetailDto?>;
