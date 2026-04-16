using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Commands.RedeemBenefitOfferByTorcedor;

public sealed class RedeemBenefitOfferByTorcedorCommandHandler(ITorcedorBenefitRedemptionPort port)
    : IRequestHandler<RedeemBenefitOfferByTorcedorCommand, TorcedorRedemptionResult>
{
    public Task<TorcedorRedemptionResult> Handle(
        RedeemBenefitOfferByTorcedorCommand request,
        CancellationToken cancellationToken) =>
        port.RedeemOfferAsync(request.OfferId, request.UserId, cancellationToken);
}
