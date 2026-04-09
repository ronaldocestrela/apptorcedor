using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Backoffice.Application.Contracts;
using SocioTorcedor.Modules.Backoffice.Application.DTOs;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;

namespace SocioTorcedor.Modules.Backoffice.Application.Queries.ListTenantsByPlan;

public sealed class ListTenantsByPlanHandler(
    ITenantPlanRepository tenantPlanRepository,
    ITenantRepository tenantRepository)
    : IQueryHandler<ListTenantsByPlanQuery, PagedResult<TenantPlanSummaryDto>>
{
    public async Task<Result<PagedResult<TenantPlanSummaryDto>>> Handle(
        ListTenantsByPlanQuery query,
        CancellationToken cancellationToken)
    {
        var page = await tenantPlanRepository.ListByPlanIdPagedAsync(
            query.PlanId,
            query.Page,
            query.PageSize,
            cancellationToken);

        var names = await tenantRepository.GetTenantNamesByIdsAsync(
            page.Items.Select(p => p.TenantId),
            cancellationToken);

        var items = page.Items
            .Select(p => new TenantPlanSummaryDto(
                p.TenantId,
                names.TryGetValue(p.TenantId, out var n) ? n : string.Empty,
                p.StartDate,
                p.Status))
            .ToList();

        var result = new PagedResult<TenantPlanSummaryDto>(items, page.TotalCount, page.Page, page.PageSize);
        return Result<PagedResult<TenantPlanSummaryDto>>.Ok(result);
    }
}
