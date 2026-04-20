using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Benefits;

public sealed class TorcedorBenefitRedemptionService(AppDbContext db) : ITorcedorBenefitRedemptionPort
{
    public async Task<TorcedorRedemptionResult> RedeemOfferAsync(
        Guid offerId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var row = await (
                from o in db.BenefitOffers.AsNoTracking()
                join p in db.BenefitPartners.AsNoTracking() on o.PartnerId equals p.Id
                where o.Id == offerId
                select new { Offer = o, Partner = p })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (row is null)
            return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.NotFound);

        if (!row.Partner.IsActive)
            return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.NotEligible);

        var now = DateTimeOffset.UtcNow;
        if (!row.Offer.IsActive || now < row.Offer.StartAt || now > row.Offer.EndAt)
            return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.NotEligible);

        var userExists = await db.Users.AsNoTracking().AnyAsync(u => u.Id == userId, cancellationToken).ConfigureAwait(false);
        if (!userExists)
            return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.NotFound);

        var planRows = await db.BenefitOfferPlanEligibilities.AsNoTracking()
            .Where(x => x.OfferId == offerId)
            .Select(x => x.PlanId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var statusRows = await db.BenefitOfferMembershipStatusEligibilities.AsNoTracking()
            .Where(x => x.OfferId == offerId)
            .Select(x => x.Status)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var membership = await db.Memberships.AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
        var snapshot = membership is null
            ? null
            : new MembershipRecordSnapshot(membership.PlanId, membership.Status);

        if (!BenefitOfferEligibility.MatchesPlanAndStatus(planRows, statusRows, snapshot))
            return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.NotEligible);

        var already = await db.BenefitRedemptions.AsNoTracking()
            .AnyAsync(r => r.OfferId == offerId && r.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
        if (already)
            return TorcedorRedemptionResult.Fail(TorcedorRedemptionError.AlreadyRedeemed);

        var redemptionId = Guid.NewGuid();
        db.BenefitRedemptions.Add(
            new BenefitRedemptionRecord
            {
                Id = redemptionId,
                OfferId = offerId,
                UserId = userId,
                ActorUserId = null,
                Notes = null,
                CreatedAt = now,
            });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return TorcedorRedemptionResult.Success(redemptionId);
    }
}
