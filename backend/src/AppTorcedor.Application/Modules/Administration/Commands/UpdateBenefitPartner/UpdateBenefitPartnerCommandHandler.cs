using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.UpdateBenefitPartner;

public sealed class UpdateBenefitPartnerCommandHandler(IBenefitsAdministrationPort benefits)
    : IRequestHandler<UpdateBenefitPartnerCommand, BenefitMutationResult>
{
    public Task<BenefitMutationResult> Handle(UpdateBenefitPartnerCommand request, CancellationToken cancellationToken) =>
        benefits.UpdatePartnerAsync(request.PartnerId, request.Dto, cancellationToken);
}
