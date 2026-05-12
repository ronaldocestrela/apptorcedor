using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Commands.CancelMyBenefitRedemption;

public sealed class CancelMyBenefitRedemptionCommandHandler(ITorcedorBenefitRedemptionPort redemption)
    : IRequestHandler<CancelMyBenefitRedemptionCommand, TorcedorRedemptionCancelResult>
{
    public Task<TorcedorRedemptionCancelResult> Handle(
        CancelMyBenefitRedemptionCommand request,
        CancellationToken cancellationToken) =>
        redemption.CancelMyRedemptionAsync(request.OfferId, request.UserId, cancellationToken);
}
