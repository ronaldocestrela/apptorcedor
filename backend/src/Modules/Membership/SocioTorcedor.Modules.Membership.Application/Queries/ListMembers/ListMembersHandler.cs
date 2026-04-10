using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Application.DTOs;

namespace SocioTorcedor.Modules.Membership.Application.Queries.ListMembers;

public sealed class ListMembersHandler(
    IMemberProfileRepository repository,
    ICurrentTenantContext tenantContext) : IQueryHandler<ListMembersQuery, PagedResult<MemberProfileDto>>
{
    public async Task<Result<PagedResult<MemberProfileDto>>> Handle(
        ListMembersQuery query,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved)
            return Result<PagedResult<MemberProfileDto>>.Fail(
                Error.Failure("Tenant.Required", "Tenant context is not resolved."));

        var page = await repository.ListAsync(query.Page, query.PageSize, query.Status, cancellationToken);
        var items = page.Items.Select(p => p.ToDto()).ToList();

        return Result<PagedResult<MemberProfileDto>>.Ok(
            new PagedResult<MemberProfileDto>(items, page.TotalCount, page.Page, page.PageSize));
    }
}
