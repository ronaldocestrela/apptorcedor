using AppTorcedor.Identity;

namespace AppTorcedor.Application.Abstractions;

public interface IMembershipWritePort
{
    Task<bool> UpdateStatusAsync(Guid membershipId, MembershipStatus status, CancellationToken cancellationToken = default);
}
