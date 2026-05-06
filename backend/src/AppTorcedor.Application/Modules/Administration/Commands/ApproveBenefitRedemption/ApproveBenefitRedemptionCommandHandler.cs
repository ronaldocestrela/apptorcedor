using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.ApproveBenefitRedemption;

public sealed class ApproveBenefitRedemptionCommandHandler(IBenefitsAdministrationPort benefits)
    : IRequestHandler<ApproveBenefitRedemptionCommand, BenefitMutationResult>
{
    public Task<BenefitMutationResult> Handle(ApproveBenefitRedemptionCommand request, CancellationToken cancellationToken) =>
        benefits.ApproveBenefitRedemptionAsync(request.RedemptionId, request.ReviewerUserId, cancellationToken);
}
