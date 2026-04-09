using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Backoffice.Application.Contracts;
using SocioTorcedor.Modules.Backoffice.Application.DTOs;

namespace SocioTorcedor.Modules.Backoffice.Application.Queries.ListSaaSPlans;

public sealed class ListSaaSPlansHandler(ISaaSPlanRepository repository)
    : IQueryHandler<ListSaaSPlansQuery, PagedResult<SaaSPlanDto>>
{
    public async Task<Result<PagedResult<SaaSPlanDto>>> Handle(
        ListSaaSPlansQuery query,
        CancellationToken cancellationToken)
    {
        var page = await repository.ListAsync(query.Page, query.PageSize, cancellationToken);
        return Result<PagedResult<SaaSPlanDto>>.Ok(page);
    }
}
