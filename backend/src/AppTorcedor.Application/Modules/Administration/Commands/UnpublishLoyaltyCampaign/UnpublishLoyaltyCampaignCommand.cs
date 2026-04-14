using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.UnpublishLoyaltyCampaign;

public sealed record UnpublishLoyaltyCampaignCommand(Guid CampaignId) : IRequest<LoyaltyMutationResult>;
