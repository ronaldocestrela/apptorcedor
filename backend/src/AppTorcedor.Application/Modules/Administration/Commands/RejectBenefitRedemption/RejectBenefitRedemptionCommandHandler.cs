using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.RejectBenefitRedemption;

public sealed class RejectBenefitRedemptionCommandHandler(IBenefitsAdministrationPort benefits)
    : IRequestHandler<RejectBenefitRedemptionCommand, BenefitMutationResult>
{
    public Task<BenefitMutationResult> Handle(
        RejectBenefitRedemptionCommand request,
        CancellationToken cancellationToken) =>
        benefits.RejectBenefitRedemptionAsync(request.RedemptionId, request.ReviewerUserId, request.Reason, cancellationToken);
}
