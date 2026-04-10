using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Application.DTOs;

namespace SocioTorcedor.Modules.Membership.Application.Queries.ListMemberPlans;

public sealed class ListMemberPlansHandler(
    IMemberPlanRepository repository,
    ICurrentTenantContext tenantContext) : IQueryHandler<ListMemberPlansQuery, PagedResult<MemberPlanDto>>
{
    public async Task<Result<PagedResult<MemberPlanDto>>> Handle(
        ListMemberPlansQuery query,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved)
            return Result<PagedResult<MemberPlanDto>>.Fail(
                Error.Failure("Tenant.Required", "Tenant context is not resolved."));

        var page = await repository.ListAsync(query.Page, query.PageSize, cancellationToken);
        var items = page.Items.Select(p => p.ToDto()).ToList();

        return Result<PagedResult<MemberPlanDto>>.Ok(
            new PagedResult<MemberPlanDto>(items, page.TotalCount, page.Page, page.PageSize));
    }
}
