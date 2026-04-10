using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Membership.Domain.Entities;
using SocioTorcedor.Modules.Membership.Domain.Enums;

namespace SocioTorcedor.Modules.Membership.Application.Contracts;

public interface IMemberProfileRepository
{
    Task<bool> ExistsByUserIdAsync(string userId, CancellationToken cancellationToken);

    Task<bool> ExistsByCpfDigitsAsync(string cpfDigits, CancellationToken cancellationToken);

    Task<MemberProfile?> GetTrackedByUserIdAsync(string userId, CancellationToken cancellationToken);

    Task<MemberProfile?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(MemberProfile profile, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<PagedResult<MemberProfile>> ListAsync(
        int page,
        int pageSize,
        MemberStatus? status,
        CancellationToken cancellationToken);
}
