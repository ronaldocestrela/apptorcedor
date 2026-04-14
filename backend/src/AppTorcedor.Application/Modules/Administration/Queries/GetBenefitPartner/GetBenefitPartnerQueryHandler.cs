using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetBenefitPartner;

public sealed class GetBenefitPartnerQueryHandler(IBenefitsAdministrationPort benefits)
    : IRequestHandler<GetBenefitPartnerQuery, BenefitPartnerDetailDto?>
{
    public Task<BenefitPartnerDetailDto?> Handle(GetBenefitPartnerQuery request, CancellationToken cancellationToken) =>
        benefits.GetPartnerByIdAsync(request.PartnerId, cancellationToken);
}
