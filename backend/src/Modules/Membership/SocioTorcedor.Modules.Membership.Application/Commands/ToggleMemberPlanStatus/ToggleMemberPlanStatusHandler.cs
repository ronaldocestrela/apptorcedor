using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Application.DTOs;

namespace SocioTorcedor.Modules.Membership.Application.Commands.ToggleMemberPlanStatus;

public sealed class ToggleMemberPlanStatusHandler(
    IMemberPlanRepository repository,
    ICurrentTenantContext tenantContext) : ICommandHandler<ToggleMemberPlanStatusCommand, MemberPlanDto>
{
    public async Task<Result<MemberPlanDto>> Handle(
        ToggleMemberPlanStatusCommand command,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved)
            return Result<MemberPlanDto>.Fail(
                Error.Failure("Tenant.Required", "Tenant context is not resolved."));

        var plan = await repository.GetTrackedByIdAsync(command.PlanId, cancellationToken);
        if (plan is null)
            return Result<MemberPlanDto>.Fail(
                Error.NotFound("Membership.PlanNotFound", "Member plan was not found."));

        plan.ToggleActive();
        await repository.SaveChangesAsync(cancellationToken);

        return Result<MemberPlanDto>.Ok(plan.ToDto());
    }
}
