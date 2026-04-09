using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Backoffice.Application.DTOs;
using SocioTorcedor.Modules.Backoffice.Domain.Entities;

namespace SocioTorcedor.Modules.Backoffice.Application.Contracts;

public interface ISaaSPlanRepository
{
    Task<SaaSPlan?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<SaaSPlanDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<SaaSPlanDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task AddAsync(SaaSPlan plan, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
