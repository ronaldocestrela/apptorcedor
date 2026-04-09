using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Tenancy.Application.DTOs;
using SocioTorcedor.Modules.Tenancy.Domain.Entities;
using SocioTorcedor.Modules.Tenancy.Domain.Enums;

namespace SocioTorcedor.Modules.Tenancy.Application.Contracts;

public interface ITenantRepository
{
    Task<TenantDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<TenantDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<TenantListItemDto>> ListAsync(
        int page,
        int pageSize,
        string? search,
        TenantStatus? status,
        CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken);

    Task AddAsync(Tenant tenant, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<Tenant?> GetEntityByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, string>> GetTenantNamesByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken);
}
