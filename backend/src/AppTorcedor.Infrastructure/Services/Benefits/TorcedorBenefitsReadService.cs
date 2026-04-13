using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Benefits;

public sealed class TorcedorBenefitsReadService(AppDbContext db) : ITorcedorBenefitsReadPort
{
    public async Task<TorcedorEligibleBenefitOffersPageDto> ListEligibleForUserAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var now = DateTimeOffset.UtcNow;

        var membership = await db.Memberships.AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
        var snapshot = membership is null
            ? null
            : new MembershipRecordSnapshot(membership.PlanId, membership.Status);

        var baseOffers = await (
                from o in db.BenefitOffers.AsNoTracking()
                join p in db.BenefitPartners.AsNoTracking() on o.PartnerId equals p.Id
                where o.IsActive && p.IsActive && now >= o.StartAt && now <= o.EndAt
                select new { Offer = o, Partner = p })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var offerIds = baseOffers.Select(x => x.Offer.Id).ToList();
        if (offerIds.Count == 0)
            return new TorcedorEligibleBenefitOffersPageDto(0, []);

        var planRows = await db.BenefitOfferPlanEligibilities.AsNoTracking()
            .Where(e => offerIds.Contains(e.OfferId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var planElig = planRows
            .GroupBy(e => e.OfferId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.PlanId).ToList());

        var statusRows = await db.BenefitOfferMembershipStatusEligibilities.AsNoTracking()
            .Where(e => offerIds.Contains(e.OfferId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var statusElig = statusRows
            .GroupBy(e => e.OfferId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Status).ToList());

        var eligible = baseOffers
            .Where(x =>
            {
                var plans = planElig.GetValueOrDefault(x.Offer.Id, []);
                var statuses = statusElig.GetValueOrDefault(x.Offer.Id, []);
                return BenefitOfferEligibility.MatchesPlanAndStatus(plans, statuses, snapshot);
            })
            .OrderByDescending(x => x.Offer.EndAt)
            .ThenByDescending(x => x.Offer.CreatedAt)
            .ToList();

        var total = eligible.Count;
        var pageItems = eligible
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TorcedorEligibleBenefitOfferItemDto(
                x.Offer.Id,
                x.Offer.PartnerId,
                x.Partner.Name,
                x.Offer.Title,
                x.Offer.Description,
                x.Offer.StartAt,
                x.Offer.EndAt))
            .ToList();

        return new TorcedorEligibleBenefitOffersPageDto(total, pageItems);
    }
}
