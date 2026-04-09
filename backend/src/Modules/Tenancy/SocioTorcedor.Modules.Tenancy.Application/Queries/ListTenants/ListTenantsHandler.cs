using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;
using SocioTorcedor.Modules.Tenancy.Application.DTOs;

namespace SocioTorcedor.Modules.Tenancy.Application.Queries.ListTenants;

public sealed class ListTenantsHandler(ITenantRepository repository)
    : IQueryHandler<ListTenantsQuery, PagedResult<TenantListItemDto>>
{
    public async Task<Result<PagedResult<TenantListItemDto>>> Handle(
        ListTenantsQuery query,
        CancellationToken cancellationToken)
    {
        var page = await repository.ListAsync(
            query.Page,
            query.PageSize,
            query.Search,
            query.Status,
            cancellationToken);

        return Result<PagedResult<TenantListItemDto>>.Ok(page);
    }
}
