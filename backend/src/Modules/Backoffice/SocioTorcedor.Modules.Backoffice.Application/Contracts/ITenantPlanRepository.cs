using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Backoffice.Domain.Entities;

namespace SocioTorcedor.Modules.Backoffice.Application.Contracts;

public interface ITenantPlanRepository
{
    Task<TenantPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<TenantPlan?> GetActiveByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken);

    Task RevokeActiveForTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<PagedResult<TenantPlan>> ListByPlanIdPagedAsync(
        Guid planId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task AddAsync(TenantPlan plan, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
