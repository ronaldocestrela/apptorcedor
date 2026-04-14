using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.ManualLoyaltyPointsAdjustment;

public sealed class ManualLoyaltyPointsAdjustmentCommandHandler(ILoyaltyAdministrationPort loyalty)
    : IRequestHandler<ManualLoyaltyPointsAdjustmentCommand, LoyaltyManualAdjustResult>
{
    public Task<LoyaltyManualAdjustResult> Handle(ManualLoyaltyPointsAdjustmentCommand request, CancellationToken cancellationToken) =>
        loyalty.ManualAdjustAsync(
            request.UserId,
            request.Points,
            request.Reason,
            request.CampaignId,
            request.ActorUserId,
            cancellationToken);
}
