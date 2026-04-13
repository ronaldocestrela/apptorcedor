using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.PublishLoyaltyCampaign;

public sealed class PublishLoyaltyCampaignCommandHandler(ILoyaltyAdministrationPort loyalty)
    : IRequestHandler<PublishLoyaltyCampaignCommand, LoyaltyMutationResult>
{
    public Task<LoyaltyMutationResult> Handle(PublishLoyaltyCampaignCommand request, CancellationToken cancellationToken) =>
        loyalty.PublishCampaignAsync(request.CampaignId, cancellationToken);
}
