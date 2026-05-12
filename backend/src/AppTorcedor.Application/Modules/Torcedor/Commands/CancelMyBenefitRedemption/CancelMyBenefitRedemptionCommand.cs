using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Commands.CancelMyBenefitRedemption;

public sealed record CancelMyBenefitRedemptionCommand(
    Guid UserId,
    Guid OfferId)
    : IRequest<TorcedorRedemptionCancelResult>;
