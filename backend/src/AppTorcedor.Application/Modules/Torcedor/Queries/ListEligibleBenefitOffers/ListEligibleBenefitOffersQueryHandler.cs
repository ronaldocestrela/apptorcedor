using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.ListEligibleBenefitOffers;

public sealed class ListEligibleBenefitOffersQueryHandler(ITorcedorBenefitsReadPort port)
    : IRequestHandler<ListEligibleBenefitOffersQuery, TorcedorEligibleBenefitOffersPageDto>
{
    public Task<TorcedorEligibleBenefitOffersPageDto> Handle(
        ListEligibleBenefitOffersQuery request,
        CancellationToken cancellationToken) =>
        port.ListEligibleForUserAsync(request.UserId, request.Page, request.PageSize, cancellationToken);
}
