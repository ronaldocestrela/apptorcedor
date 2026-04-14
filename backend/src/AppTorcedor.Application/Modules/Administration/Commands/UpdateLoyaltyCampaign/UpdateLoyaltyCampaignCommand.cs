using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.UpdateLoyaltyCampaign;

public sealed record UpdateLoyaltyCampaignCommand(Guid CampaignId, LoyaltyCampaignWriteDto Dto) : IRequest<LoyaltyMutationResult>;
