using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.UpdateBenefitOffer;

public sealed class UpdateBenefitOfferCommandHandler(IBenefitsAdministrationPort benefits)
    : IRequestHandler<UpdateBenefitOfferCommand, BenefitMutationResult>
{
    public Task<BenefitMutationResult> Handle(UpdateBenefitOfferCommand request, CancellationToken cancellationToken) =>
        benefits.UpdateOfferAsync(request.OfferId, request.Dto, cancellationToken);
}
