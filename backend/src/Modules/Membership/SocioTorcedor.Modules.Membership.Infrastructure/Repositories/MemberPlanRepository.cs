using Microsoft.EntityFrameworkCore;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Domain.Entities;
using SocioTorcedor.Modules.Membership.Infrastructure.Persistence;

namespace SocioTorcedor.Modules.Membership.Infrastructure.Repositories;

public sealed class MemberPlanRepository(TenantMembershipDbContext db) : IMemberPlanRepository
{
    public Task<bool> ExistsByNameAsync(string name, Guid? excludingId, CancellationToken cancellationToken) =>
        db.MemberPlans.AnyAsync(
            p => p.Nome == name && (!excludingId.HasValue || p.Id != excludingId.Value),
            cancellationToken);

    public Task<MemberPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.MemberPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<MemberPlan?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.MemberPlans.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task AddAsync(MemberPlan plan, CancellationToken cancellationToken) =>
        await db.MemberPlans.AddAsync(plan, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task<PagedResult<MemberPlan>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.MemberPlans.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<MemberPlan>(items, total, page, pageSize);
    }
}
