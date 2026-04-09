using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Backoffice.Application.Contracts;
using SocioTorcedor.Modules.Backoffice.Domain.Entities;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;

namespace SocioTorcedor.Modules.Backoffice.Application.Commands.AssignPlanToTenant;

public sealed class AssignPlanToTenantHandler(
    ITenantRepository tenantRepository,
    ISaaSPlanRepository saaSPlanRepository,
    ITenantPlanRepository tenantPlanRepository)
    : ICommandHandler<AssignPlanToTenantCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AssignPlanToTenantCommand command, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(command.TenantId, cancellationToken);
        if (tenant is null)
            return Result<Guid>.Fail(Error.NotFound("Tenant.NotFound", "Tenant not found."));

        var plan = await saaSPlanRepository.GetTrackedByIdAsync(command.SaaSPlanId, cancellationToken);
        if (plan is null)
            return Result<Guid>.Fail(Error.NotFound("SaaSPlan.NotFound", "SaaS plan not found."));

        if (!plan.IsActive)
            return Result<Guid>.Fail(Error.Failure("SaaSPlan.Inactive", "Cannot assign an inactive SaaS plan."));

        await tenantPlanRepository.RevokeActiveForTenantAsync(command.TenantId, cancellationToken);

        var assignment = TenantPlan.Assign(
            command.TenantId,
            command.SaaSPlanId,
            command.StartDate,
            command.EndDate,
            command.BillingCycle);

        await tenantPlanRepository.AddAsync(assignment, cancellationToken);
        await tenantPlanRepository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Ok(assignment.Id);
    }
}
