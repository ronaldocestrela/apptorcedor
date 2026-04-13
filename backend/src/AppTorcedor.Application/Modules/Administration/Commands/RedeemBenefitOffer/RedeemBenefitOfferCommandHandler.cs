using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.RedeemBenefitOffer;

public sealed class RedeemBenefitOfferCommandHandler(IBenefitsAdministrationPort benefits)
    : IRequestHandler<RedeemBenefitOfferCommand, BenefitRedeemResult>
{
    public Task<BenefitRedeemResult> Handle(RedeemBenefitOfferCommand request, CancellationToken cancellationToken) =>
        benefits.RedeemOfferAsync(request.OfferId, request.UserId, request.Notes, request.ActorUserId, cancellationToken);
}
