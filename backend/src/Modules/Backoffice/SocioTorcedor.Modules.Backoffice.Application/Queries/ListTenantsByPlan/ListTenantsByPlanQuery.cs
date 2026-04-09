using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Backoffice.Application.DTOs;

namespace SocioTorcedor.Modules.Backoffice.Application.Queries.ListTenantsByPlan;

public sealed record ListTenantsByPlanQuery(Guid PlanId, int Page, int PageSize)
    : IQuery<PagedResult<TenantPlanSummaryDto>>;
