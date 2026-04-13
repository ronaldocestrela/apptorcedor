using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListBenefitRedemptions;

public sealed class ListBenefitRedemptionsQueryHandler(IBenefitsAdministrationPort benefits)
    : IRequestHandler<ListBenefitRedemptionsQuery, BenefitRedemptionListPageDto>
{
    public Task<BenefitRedemptionListPageDto> Handle(ListBenefitRedemptionsQuery request, CancellationToken cancellationToken) =>
        benefits.ListRedemptionsAsync(request.OfferId, request.UserId, request.Page, request.PageSize, cancellationToken);
}
