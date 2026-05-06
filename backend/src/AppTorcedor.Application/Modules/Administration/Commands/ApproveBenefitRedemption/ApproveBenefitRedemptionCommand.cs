using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.ApproveBenefitRedemption;

public sealed record ApproveBenefitRedemptionCommand(Guid RedemptionId, Guid ReviewerUserId) : IRequest<BenefitMutationResult>;
