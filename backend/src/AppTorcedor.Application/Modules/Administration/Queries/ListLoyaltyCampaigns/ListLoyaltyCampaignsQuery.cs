using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListLoyaltyCampaigns;

public sealed record ListLoyaltyCampaignsQuery(LoyaltyCampaignStatus? Status, int Page, int PageSize)
    : IRequest<LoyaltyCampaignListPageDto>;
