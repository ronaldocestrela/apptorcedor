using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Plans;

public sealed class TorcedorPublishedPlansReadService(AppDbContext db) : ITorcedorPublishedPlansReadPort
{
    public async Task<TorcedorPublishedPlansCatalogDto> ListPublishedActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var plans = await db.MembershipPlans.AsNoTracking()
            .Where(p => p.IsPublished && p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name, p.Price, p.BillingCycle, p.DiscountPercentage, p.Summary })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (plans.Count == 0)
            return new TorcedorPublishedPlansCatalogDto([]);

        var planIds = plans.Select(p => p.Id).ToList();
        var benefitRows = await db.MembershipPlanBenefits.AsNoTracking()
            .Where(b => planIds.Contains(b.PlanId))
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Title)
            .Select(b => new { b.Id, b.PlanId, b.Title, b.Description })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var benefitsByPlan = benefitRows
            .GroupBy(b => b.PlanId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var items = plans
            .Select(p =>
            {
                var benefits = benefitsByPlan.TryGetValue(p.Id, out var list)
                    ? list.Select(b => new TorcedorPublishedPlanBenefitDto(b.Id, b.Title, b.Description)).ToList()
                    : (IReadOnlyList<TorcedorPublishedPlanBenefitDto>)[];
                return new TorcedorPublishedPlanItemDto(
                    p.Id,
                    p.Name,
                    p.Price,
                    p.BillingCycle,
                    p.DiscountPercentage,
                    p.Summary,
                    benefits);
            })
            .ToList();

        return new TorcedorPublishedPlansCatalogDto(items);
    }
}
