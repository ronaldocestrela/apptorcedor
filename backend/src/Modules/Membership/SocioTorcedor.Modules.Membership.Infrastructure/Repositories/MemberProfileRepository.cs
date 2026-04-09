using Microsoft.EntityFrameworkCore;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Domain.Entities;
using SocioTorcedor.Modules.Membership.Infrastructure.Persistence;

namespace SocioTorcedor.Modules.Membership.Infrastructure.Repositories;

public sealed class MemberProfileRepository(TenantMembershipDbContext db) : IMemberProfileRepository
{
    public Task<bool> ExistsByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        db.MemberProfiles.AnyAsync(p => p.UserId == userId, cancellationToken);

    public Task<bool> ExistsByCpfDigitsAsync(string cpfDigits, CancellationToken cancellationToken) =>
        db.MemberProfiles.AnyAsync(p => p.CpfDigits == cpfDigits, cancellationToken);

    public Task<MemberProfile?> GetTrackedByUserIdAsync(string userId, CancellationToken cancellationToken) =>
        db.MemberProfiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public Task<MemberProfile?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.MemberProfiles.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task AddAsync(MemberProfile profile, CancellationToken cancellationToken) =>
        await db.MemberProfiles.AddAsync(profile, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task<PagedResult<MemberProfile>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.MemberProfiles.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<MemberProfile>(items, total, page, pageSize);
    }
}
