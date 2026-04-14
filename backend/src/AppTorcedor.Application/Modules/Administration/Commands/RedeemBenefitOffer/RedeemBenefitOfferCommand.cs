using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.RedeemBenefitOffer;

public sealed record RedeemBenefitOfferCommand(
    Guid OfferId,
    Guid UserId,
    string? Notes,
    Guid ActorUserId) : IRequest<BenefitRedeemResult>;
