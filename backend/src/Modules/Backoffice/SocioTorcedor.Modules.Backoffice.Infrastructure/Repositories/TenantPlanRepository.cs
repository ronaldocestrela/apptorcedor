using Microsoft.EntityFrameworkCore;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Backoffice.Application.Contracts;
using SocioTorcedor.Modules.Backoffice.Domain.Entities;
using SocioTorcedor.Modules.Backoffice.Domain.Enums;
using SocioTorcedor.Modules.Backoffice.Infrastructure.Persistence;

namespace SocioTorcedor.Modules.Backoffice.Infrastructure.Repositories;

public sealed class TenantPlanRepository(BackofficeMasterDbContext db) : ITenantPlanRepository
{
    public async Task<TenantPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await db.TenantPlans.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<TenantPlan?> GetActiveByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await db.TenantPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.TenantId == tenantId && t.Status == TenantPlanStatus.Active,
                cancellationToken);

    public async Task RevokeActiveForTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var actives = await db.TenantPlans
            .Where(t => t.TenantId == tenantId && t.Status == TenantPlanStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var a in actives)
            a.Revoke();
    }

    public async Task<PagedResult<TenantPlan>> ListByPlanIdPagedAsync(
        Guid planId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.TenantPlans
            .AsNoTracking()
            .Where(t => t.SaaSPlanId == planId);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TenantPlan>(items, total, page, pageSize);
    }

    public async Task AddAsync(TenantPlan plan, CancellationToken cancellationToken) =>
        await db.TenantPlans.AddAsync(plan, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
