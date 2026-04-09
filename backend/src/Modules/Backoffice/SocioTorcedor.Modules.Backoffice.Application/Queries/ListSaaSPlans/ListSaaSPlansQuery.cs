using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Backoffice.Application.DTOs;

namespace SocioTorcedor.Modules.Backoffice.Application.Queries.ListSaaSPlans;

public sealed record ListSaaSPlansQuery(int Page, int PageSize) : IQuery<PagedResult<SaaSPlanDto>>;
