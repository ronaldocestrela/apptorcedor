using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Backoffice.Application.Contracts;
using SocioTorcedor.Modules.Backoffice.Application.DTOs;

namespace SocioTorcedor.Modules.Backoffice.Application.Queries.GetTenantPlan;

public sealed class GetTenantPlanHandler(
    ITenantPlanRepository tenantPlanRepository,
    ISaaSPlanRepository saaSPlanRepository)
    : IQueryHandler<GetTenantPlanQuery, TenantPlanDto>
{
    public async Task<Result<TenantPlanDto>> Handle(
        GetTenantPlanQuery query,
        CancellationToken cancellationToken)
    {
        var assignment = await tenantPlanRepository.GetActiveByTenantIdAsync(query.TenantId, cancellationToken);
        if (assignment is null)
            return Result<TenantPlanDto>.Fail(Error.NotFound("TenantPlan.NotFound", "No active plan for this tenant."));

        var plan = await saaSPlanRepository.GetDetailByIdAsync(assignment.SaaSPlanId, cancellationToken);
        var planName = plan?.Name ?? string.Empty;

        var dto = new TenantPlanDto(
            assignment.Id,
            assignment.TenantId,
            assignment.SaaSPlanId,
            planName,
            assignment.StartDate,
            assignment.EndDate,
            assignment.Status,
            assignment.BillingCycle);

        return Result<TenantPlanDto>.Ok(dto);
    }
}
