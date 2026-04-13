using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListBenefitPartners;

public sealed class ListBenefitPartnersQueryHandler(IBenefitsAdministrationPort benefits)
    : IRequestHandler<ListBenefitPartnersQuery, BenefitPartnerListPageDto>
{
    public Task<BenefitPartnerListPageDto> Handle(ListBenefitPartnersQuery request, CancellationToken cancellationToken) =>
        benefits.ListPartnersAsync(request.Search, request.IsActive, request.Page, request.PageSize, cancellationToken);
}
