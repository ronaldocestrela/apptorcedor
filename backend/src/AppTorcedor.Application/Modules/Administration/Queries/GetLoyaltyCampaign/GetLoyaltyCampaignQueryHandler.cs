using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetLoyaltyCampaign;

public sealed class GetLoyaltyCampaignQueryHandler(ILoyaltyAdministrationPort loyalty)
    : IRequestHandler<GetLoyaltyCampaignQuery, LoyaltyCampaignDetailDto?>
{
    public Task<LoyaltyCampaignDetailDto?> Handle(GetLoyaltyCampaignQuery request, CancellationToken cancellationToken) =>
        loyalty.GetCampaignByIdAsync(request.CampaignId, cancellationToken);
}
