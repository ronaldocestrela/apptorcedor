using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.ReplaceBenefitOfferShirtCatalog;

public sealed class ReplaceBenefitOfferShirtCatalogCommandHandler(IBenefitsAdministrationPort benefits)
    : IRequestHandler<ReplaceBenefitOfferShirtCatalogCommand, BenefitMutationResult>
{
    public Task<BenefitMutationResult> Handle(
        ReplaceBenefitOfferShirtCatalogCommand request,
        CancellationToken cancellationToken) =>
        benefits.ReplaceShirtCatalogAsync(request.OfferId, request.Sizes, request.Models, cancellationToken);
}
