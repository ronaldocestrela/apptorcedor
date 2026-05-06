using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.RejectBenefitRedemption;

public sealed record RejectBenefitRedemptionCommand(Guid RedemptionId, Guid ReviewerUserId, string? Reason)
    : IRequest<BenefitMutationResult>;
