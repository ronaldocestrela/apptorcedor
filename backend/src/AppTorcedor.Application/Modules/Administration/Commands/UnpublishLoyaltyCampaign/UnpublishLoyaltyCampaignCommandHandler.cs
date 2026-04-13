using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.UnpublishLoyaltyCampaign;

public sealed class UnpublishLoyaltyCampaignCommandHandler(ILoyaltyAdministrationPort loyalty)
    : IRequestHandler<UnpublishLoyaltyCampaignCommand, LoyaltyMutationResult>
{
    public Task<LoyaltyMutationResult> Handle(UnpublishLoyaltyCampaignCommand request, CancellationToken cancellationToken) =>
        loyalty.UnpublishCampaignAsync(request.CampaignId, cancellationToken);
}
