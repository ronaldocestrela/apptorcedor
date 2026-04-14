using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.PublishLoyaltyCampaign;

public sealed record PublishLoyaltyCampaignCommand(Guid CampaignId) : IRequest<LoyaltyMutationResult>;
