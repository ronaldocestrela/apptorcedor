using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.ManualLoyaltyPointsAdjustment;

public sealed record ManualLoyaltyPointsAdjustmentCommand(
    Guid UserId,
    int Points,
    string Reason,
    Guid? CampaignId,
    Guid ActorUserId) : IRequest<LoyaltyManualAdjustResult>;
