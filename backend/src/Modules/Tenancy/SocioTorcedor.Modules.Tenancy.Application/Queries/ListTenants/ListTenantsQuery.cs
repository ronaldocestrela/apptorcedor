using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Tenancy.Application.DTOs;
using SocioTorcedor.Modules.Tenancy.Domain.Enums;

namespace SocioTorcedor.Modules.Tenancy.Application.Queries.ListTenants;

public sealed record ListTenantsQuery(
    int Page,
    int PageSize,
    string? Search,
    TenantStatus? Status) : IQuery<PagedResult<TenantListItemDto>>;
