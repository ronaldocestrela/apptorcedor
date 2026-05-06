using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.ReplaceBenefitOfferShirtCatalog;

public sealed record ReplaceBenefitOfferShirtCatalogCommand(
    Guid OfferId,
    IReadOnlyList<string> Sizes,
    IReadOnlyList<string> Models)
    : IRequest<BenefitMutationResult>;
