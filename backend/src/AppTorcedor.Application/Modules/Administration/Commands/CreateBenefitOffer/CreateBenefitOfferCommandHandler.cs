using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.CreateBenefitOffer;

public sealed class CreateBenefitOfferCommandHandler(IBenefitsAdministrationPort benefits)
    : IRequestHandler<CreateBenefitOfferCommand, BenefitCreateResult>
{
    public Task<BenefitCreateResult> Handle(CreateBenefitOfferCommand request, CancellationToken cancellationToken) =>
        benefits.CreateOfferAsync(request.Dto, cancellationToken);
}
