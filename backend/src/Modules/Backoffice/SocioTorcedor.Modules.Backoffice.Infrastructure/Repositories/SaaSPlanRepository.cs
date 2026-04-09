using Microsoft.EntityFrameworkCore;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Backoffice.Application.Contracts;
using SocioTorcedor.Modules.Backoffice.Application.DTOs;
using SocioTorcedor.Modules.Backoffice.Domain.Entities;
using SocioTorcedor.Modules.Backoffice.Infrastructure.Persistence;

namespace SocioTorcedor.Modules.Backoffice.Infrastructure.Repositories;

public sealed class SaaSPlanRepository(BackofficeMasterDbContext db) : ISaaSPlanRepository
{
    public async Task<SaaSPlan?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await db.SaaSPlans
            .Include(p => p.Features)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<SaaSPlanDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var plan = await db.SaaSPlans
            .AsNoTracking()
            .Include(p => p.Features)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (plan is null)
            return null;

        var features = plan.Features
            .Select(f => new SaaSPlanFeatureDto(f.Key, f.Description, f.Value))
            .ToList();

        return new SaaSPlanDetailDto(
            plan.Id,
            plan.Name,
            plan.Description,
            plan.MonthlyPrice,
            plan.YearlyPrice,
            plan.MaxMembers,
            plan.IsActive,
            plan.CreatedAt,
            plan.UpdatedAt,
            features);
    }

    public async Task<PagedResult<SaaSPlanDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.SaaSPlans.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new SaaSPlanDto(
                p.Id,
                p.Name,
                p.Description,
                p.MonthlyPrice,
                p.YearlyPrice,
                p.MaxMembers,
                p.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResult<SaaSPlanDto>(items, total, page, pageSize);
    }

    public async Task AddAsync(SaaSPlan plan, CancellationToken cancellationToken) =>
        await db.SaaSPlans.AddAsync(plan, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
