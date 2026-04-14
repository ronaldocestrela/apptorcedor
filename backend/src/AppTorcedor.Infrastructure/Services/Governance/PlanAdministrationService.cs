using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Governance;

public sealed class PlanAdministrationService(AppDbContext db) : IPlansAdministrationPort
{
    public async Task<AdminPlanListPageDto> ListPlansAsync(
        string? search,
        bool? isActive,
        bool? isPublished,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.MembershipPlans.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.Name.Contains(term));
        }

        if (isActive is { } ia)
            query = query.Where(p => p.IsActive == ia);
        if (isPublished is { } ip)
            query = query.Where(p => p.IsPublished == ip);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var pageRows = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (pageRows.Count == 0)
            return new AdminPlanListPageDto(total, []);

        var ids = pageRows.Select(p => p.Id).ToList();
        var counts = await db.MembershipPlanBenefits.AsNoTracking()
            .Where(b => ids.Contains(b.PlanId))
            .GroupBy(b => b.PlanId)
            .Select(g => new { g.Key, Cnt = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Cnt, cancellationToken)
            .ConfigureAwait(false);

        var items = pageRows
            .Select(p => new AdminPlanListItemDto(
                p.Id,
                p.Name,
                p.Price,
                p.BillingCycle,
                p.DiscountPercentage,
                p.IsActive,
                p.IsPublished,
                p.PublishedAt,
                counts.TryGetValue(p.Id, out var c) ? c : 0))
            .ToList();

        return new AdminPlanListPageDto(total, items);
    }

    public async Task<AdminPlanDetailDto?> GetPlanByIdAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await db.MembershipPlans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken)
            .ConfigureAwait(false);
        if (plan is null)
            return null;

        var benefits = await db.MembershipPlanBenefits.AsNoTracking()
            .Where(b => b.PlanId == planId)
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Title)
            .Select(b => new AdminPlanBenefitDto(b.Id, b.SortOrder, b.Title, b.Description))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new AdminPlanDetailDto(
            plan.Id,
            plan.Name,
            plan.Price,
            plan.BillingCycle,
            plan.DiscountPercentage,
            plan.IsActive,
            plan.IsPublished,
            plan.PublishedAt,
            plan.Summary,
            plan.RulesNotes,
            benefits);
    }

    public async Task<Guid> CreatePlanAsync(AdminPlanWriteDto dto, CancellationToken cancellationToken = default)
    {
        var utc = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();
        var plan = new MembershipPlanRecord
        {
            Id = id,
            Name = dto.Name.Trim(),
            Price = dto.Price,
            BillingCycle = dto.BillingCycle,
            DiscountPercentage = dto.DiscountPercentage,
            IsActive = dto.IsActive,
            IsPublished = dto.IsPublished,
            PublishedAt = dto.IsPublished ? utc : null,
            Summary = string.IsNullOrWhiteSpace(dto.Summary) ? null : dto.Summary.Trim(),
            RulesNotes = string.IsNullOrWhiteSpace(dto.RulesNotes) ? null : dto.RulesNotes.Trim(),
        };

        db.MembershipPlans.Add(plan);
        AddBenefits(id, dto.Benefits);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }

    public async Task<bool> UpdatePlanAsync(Guid planId, AdminPlanWriteDto dto, CancellationToken cancellationToken = default)
    {
        var plan = await db.MembershipPlans.FirstOrDefaultAsync(p => p.Id == planId, cancellationToken).ConfigureAwait(false);
        if (plan is null)
            return false;

        var existingBenefits = await db.MembershipPlanBenefits.Where(b => b.PlanId == planId).ToListAsync(cancellationToken).ConfigureAwait(false);
        db.MembershipPlanBenefits.RemoveRange(existingBenefits);

        var utc = DateTimeOffset.UtcNow;
        var wasPublished = plan.IsPublished;

        plan.Name = dto.Name.Trim();
        plan.Price = dto.Price;
        plan.BillingCycle = dto.BillingCycle;
        plan.DiscountPercentage = dto.DiscountPercentage;
        plan.IsActive = dto.IsActive;
        plan.IsPublished = dto.IsPublished;
        plan.Summary = string.IsNullOrWhiteSpace(dto.Summary) ? null : dto.Summary.Trim();
        plan.RulesNotes = string.IsNullOrWhiteSpace(dto.RulesNotes) ? null : dto.RulesNotes.Trim();

        if (dto.IsPublished)
            plan.PublishedAt = wasPublished ? plan.PublishedAt ?? utc : utc;
        else
            plan.PublishedAt = null;

        AddBenefits(planId, dto.Benefits);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    private void AddBenefits(Guid planId, IReadOnlyList<AdminPlanBenefitInputDto> benefits)
    {
        foreach (var b in benefits)
        {
            db.MembershipPlanBenefits.Add(
                new MembershipPlanBenefitRecord
                {
                    Id = Guid.NewGuid(),
                    PlanId = planId,
                    SortOrder = b.SortOrder,
                    Title = b.Title.Trim(),
                    Description = string.IsNullOrWhiteSpace(b.Description) ? null : b.Description.Trim(),
                });
        }
    }
}
