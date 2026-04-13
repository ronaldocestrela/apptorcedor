using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Governance;

public sealed class MembershipWritePort(AppDbContext db) : IMembershipWritePort
{
    public async Task<bool> UpdateStatusAsync(Guid membershipId, MembershipStatus status, CancellationToken cancellationToken = default)
    {
        var membership = await db.Memberships.FirstOrDefaultAsync(m => m.Id == membershipId, cancellationToken).ConfigureAwait(false);
        if (membership is null)
            return false;
        membership.Status = status;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}
