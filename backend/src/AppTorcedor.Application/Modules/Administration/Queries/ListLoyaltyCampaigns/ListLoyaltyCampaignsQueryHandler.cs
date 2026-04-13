using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListLoyaltyCampaigns;

public sealed class ListLoyaltyCampaignsQueryHandler(ILoyaltyAdministrationPort loyalty)
    : IRequestHandler<ListLoyaltyCampaignsQuery, LoyaltyCampaignListPageDto>
{
    public Task<LoyaltyCampaignListPageDto> Handle(ListLoyaltyCampaignsQuery request, CancellationToken cancellationToken) =>
        loyalty.ListCampaignsAsync(request.Status, request.Page, request.PageSize, cancellationToken);
}
