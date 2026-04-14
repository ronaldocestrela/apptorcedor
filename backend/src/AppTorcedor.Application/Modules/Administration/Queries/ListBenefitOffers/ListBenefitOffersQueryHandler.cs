using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListBenefitOffers;

public sealed class ListBenefitOffersQueryHandler(IBenefitsAdministrationPort benefits)
    : IRequestHandler<ListBenefitOffersQuery, BenefitOfferListPageDto>
{
    public Task<BenefitOfferListPageDto> Handle(ListBenefitOffersQuery request, CancellationToken cancellationToken) =>
        benefits.ListOffersAsync(request.PartnerId, request.IsActive, request.Page, request.PageSize, cancellationToken);
}
