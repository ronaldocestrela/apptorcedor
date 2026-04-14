using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetLoyaltyCampaign;

public sealed record GetLoyaltyCampaignQuery(Guid CampaignId) : IRequest<LoyaltyCampaignDetailDto?>;
