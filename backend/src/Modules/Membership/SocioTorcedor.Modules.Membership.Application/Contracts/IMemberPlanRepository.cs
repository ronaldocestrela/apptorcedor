using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Membership.Domain.Entities;

namespace SocioTorcedor.Modules.Membership.Application.Contracts;

public interface IMemberPlanRepository
{
    Task<bool> ExistsByNameAsync(string name, Guid? excludingId, CancellationToken cancellationToken);

    Task<MemberPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<MemberPlan?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(MemberPlan plan, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<PagedResult<MemberPlan>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
}
