using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Commands.RedeemBenefitOfferByTorcedor;

public sealed record RedeemBenefitOfferByTorcedorCommand(Guid UserId, Guid OfferId)
    : IRequest<TorcedorRedemptionResult>;
