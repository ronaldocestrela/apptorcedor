using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.UpdateLoyaltyCampaign;

public sealed class UpdateLoyaltyCampaignCommandHandler(ILoyaltyAdministrationPort loyalty)
    : IRequestHandler<UpdateLoyaltyCampaignCommand, LoyaltyMutationResult>
{
    public Task<LoyaltyMutationResult> Handle(UpdateLoyaltyCampaignCommand request, CancellationToken cancellationToken) =>
        loyalty.UpdateCampaignAsync(request.CampaignId, request.Dto, cancellationToken);
}
