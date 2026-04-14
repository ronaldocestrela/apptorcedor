using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListBenefitOffers;

public sealed record ListBenefitOffersQuery(Guid? PartnerId, bool? IsActive, int Page, int PageSize)
    : IRequest<BenefitOfferListPageDto>;
