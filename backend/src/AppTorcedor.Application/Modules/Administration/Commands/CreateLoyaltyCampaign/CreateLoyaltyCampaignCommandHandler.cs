using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.CreateLoyaltyCampaign;

public sealed class CreateLoyaltyCampaignCommandHandler(ILoyaltyAdministrationPort loyalty)
    : IRequestHandler<CreateLoyaltyCampaignCommand, LoyaltyCampaignCreateResult>
{
    public Task<LoyaltyCampaignCreateResult> Handle(CreateLoyaltyCampaignCommand request, CancellationToken cancellationToken) =>
        loyalty.CreateCampaignAsync(request.Dto, cancellationToken);
}
