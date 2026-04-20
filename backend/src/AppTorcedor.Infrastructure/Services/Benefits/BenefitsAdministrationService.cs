using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Benefits;

public sealed class BenefitsAdministrationService(AppDbContext db, IBenefitOfferBannerStorage bannerStorage)
    : IBenefitsAdministrationPort
{
    public async Task<BenefitPartnerListPageDto> ListPartnersAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.BenefitPartners.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(p => p.Name.Contains(s));
        }

        if (isActive is { } ia)
            query = query.Where(p => p.IsActive == ia);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = rows
            .Select(p => new BenefitPartnerListItemDto(p.Id, p.Name, p.IsActive, p.CreatedAt))
            .ToList();
        return new BenefitPartnerListPageDto(total, items);
    }

    public async Task<BenefitPartnerDetailDto?> GetPartnerByIdAsync(Guid partnerId, CancellationToken cancellationToken = default)
    {
        var p = await db.BenefitPartners.AsNoTracking().FirstOrDefaultAsync(x => x.Id == partnerId, cancellationToken).ConfigureAwait(false);
        if (p is null)
            return null;
        return new BenefitPartnerDetailDto(p.Id, p.Name, p.Description, p.IsActive, p.CreatedAt, p.UpdatedAt);
    }

    public async Task<BenefitCreateResult> CreatePartnerAsync(BenefitPartnerWriteDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return new BenefitCreateResult(null, BenefitMutationError.Validation);

        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();
        db.BenefitPartners.Add(
            new BenefitPartnerRecord
            {
                Id = id,
                Name = dto.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                IsActive = dto.IsActive,
                CreatedAt = now,
                UpdatedAt = now,
            });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new BenefitCreateResult(id, null);
    }

    public async Task<BenefitMutationResult> UpdatePartnerAsync(
        Guid partnerId,
        BenefitPartnerWriteDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BenefitMutationResult.Fail(BenefitMutationError.Validation);

        var p = await db.BenefitPartners.FirstOrDefaultAsync(x => x.Id == partnerId, cancellationToken).ConfigureAwait(false);
        if (p is null)
            return BenefitMutationResult.Fail(BenefitMutationError.NotFound);

        var now = DateTimeOffset.UtcNow;
        p.Name = dto.Name.Trim();
        p.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        p.IsActive = dto.IsActive;
        p.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return BenefitMutationResult.Success();
    }

    public async Task<BenefitOfferListPageDto> ListOffersAsync(
        Guid? partnerId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query =
            from o in db.BenefitOffers.AsNoTracking()
            join p in db.BenefitPartners.AsNoTracking() on o.PartnerId equals p.Id
            select new { o, p };

        if (partnerId is { } pid)
            query = query.Where(x => x.o.PartnerId == pid);
        if (isActive is { } ia)
            query = query.Where(x => x.o.IsActive == ia);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await query
            .OrderByDescending(x => x.o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = rows
            .Select(x => new BenefitOfferListItemDto(
                x.o.Id,
                x.o.PartnerId,
                x.p.Name,
                x.o.Title,
                x.o.IsActive,
                x.o.StartAt,
                x.o.EndAt,
                x.o.CreatedAt,
                x.o.BannerUrl))
            .ToList();

        return new BenefitOfferListPageDto(total, items);
    }

    public async Task<BenefitOfferDetailDto?> GetOfferByIdAsync(Guid offerId, CancellationToken cancellationToken = default)
    {
        var o = await db.BenefitOffers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == offerId, cancellationToken).ConfigureAwait(false);
        if (o is null)
            return null;

        var planIds = await db.BenefitOfferPlanEligibilities.AsNoTracking()
            .Where(x => x.OfferId == offerId)
            .Select(x => x.PlanId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var statuses = await db.BenefitOfferMembershipStatusEligibilities.AsNoTracking()
            .Where(x => x.OfferId == offerId)
            .Select(x => x.Status)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new BenefitOfferDetailDto(
            o.Id,
            o.PartnerId,
            o.Title,
            o.Description,
            o.IsActive,
            o.StartAt,
            o.EndAt,
            o.CreatedAt,
            o.UpdatedAt,
            planIds,
            statuses,
            o.BannerUrl);
    }

    public async Task<BenefitCreateResult> CreateOfferAsync(BenefitOfferWriteDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return new BenefitCreateResult(null, BenefitMutationError.Validation);
        if (dto.EndAt < dto.StartAt)
            return new BenefitCreateResult(null, BenefitMutationError.Validation);

        var partnerExists = await db.BenefitPartners.AnyAsync(p => p.Id == dto.PartnerId, cancellationToken).ConfigureAwait(false);
        if (!partnerExists)
            return new BenefitCreateResult(null, BenefitMutationError.Validation);

        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();
        db.BenefitOffers.Add(
            new BenefitOfferRecord
            {
                Id = id,
                PartnerId = dto.PartnerId,
                Title = dto.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                IsActive = dto.IsActive,
                StartAt = dto.StartAt,
                EndAt = dto.EndAt,
                BannerUrl = null,
                CreatedAt = now,
                UpdatedAt = now,
            });

        ReplaceEligibilities(id, dto.EligiblePlanIds, dto.EligibleMembershipStatuses);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new BenefitCreateResult(id, null);
    }

    public async Task<BenefitMutationResult> UpdateOfferAsync(
        Guid offerId,
        BenefitOfferWriteDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BenefitMutationResult.Fail(BenefitMutationError.Validation);
        if (dto.EndAt < dto.StartAt)
            return BenefitMutationResult.Fail(BenefitMutationError.Validation);

        var o = await db.BenefitOffers.FirstOrDefaultAsync(x => x.Id == offerId, cancellationToken).ConfigureAwait(false);
        if (o is null)
            return BenefitMutationResult.Fail(BenefitMutationError.NotFound);

        var partnerExists = await db.BenefitPartners.AnyAsync(p => p.Id == dto.PartnerId, cancellationToken).ConfigureAwait(false);
        if (!partnerExists)
            return BenefitMutationResult.Fail(BenefitMutationError.Validation);

        var now = DateTimeOffset.UtcNow;
        o.PartnerId = dto.PartnerId;
        o.Title = dto.Title.Trim();
        o.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        o.IsActive = dto.IsActive;
        o.StartAt = dto.StartAt;
        o.EndAt = dto.EndAt;
        o.UpdatedAt = now;

        var oldPlans = await db.BenefitOfferPlanEligibilities.Where(x => x.OfferId == offerId).ToListAsync(cancellationToken).ConfigureAwait(false);
        db.BenefitOfferPlanEligibilities.RemoveRange(oldPlans);
        var oldStatuses = await db.BenefitOfferMembershipStatusEligibilities.Where(x => x.OfferId == offerId).ToListAsync(cancellationToken).ConfigureAwait(false);
        db.BenefitOfferMembershipStatusEligibilities.RemoveRange(oldStatuses);

        ReplaceEligibilities(offerId, dto.EligiblePlanIds, dto.EligibleMembershipStatuses);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return BenefitMutationResult.Success();
    }

    public async Task<BenefitBannerUploadResult> UploadOfferBannerAsync(
        Guid offerId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var o = await db.BenefitOffers.FirstOrDefaultAsync(x => x.Id == offerId, cancellationToken).ConfigureAwait(false);
        if (o is null)
            return BenefitBannerUploadResult.Fail(BenefitMutationError.NotFound);

        var url = await bannerStorage.SaveBannerAsync(content, fileName, contentType, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(url))
            return BenefitBannerUploadResult.Fail(BenefitMutationError.Validation);

        var previous = o.BannerUrl;
        var now = DateTimeOffset.UtcNow;
        o.BannerUrl = url;
        o.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await bannerStorage.TryDeleteBannerAsync(previous, cancellationToken).ConfigureAwait(false);
        return BenefitBannerUploadResult.Success(url);
    }

    public async Task<BenefitMutationResult> RemoveOfferBannerAsync(Guid offerId, CancellationToken cancellationToken = default)
    {
        var o = await db.BenefitOffers.FirstOrDefaultAsync(x => x.Id == offerId, cancellationToken).ConfigureAwait(false);
        if (o is null)
            return BenefitMutationResult.Fail(BenefitMutationError.NotFound);

        var previous = o.BannerUrl;
        o.BannerUrl = null;
        o.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await bannerStorage.TryDeleteBannerAsync(previous, cancellationToken).ConfigureAwait(false);
        return BenefitMutationResult.Success();
    }

    public async Task<BenefitRedeemResult> RedeemOfferAsync(
        Guid offerId,
        Guid userId,
        string? notes,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var offer = await db.BenefitOffers.FirstOrDefaultAsync(x => x.Id == offerId, cancellationToken).ConfigureAwait(false);
        if (offer is null)
            return BenefitRedeemResult.Fail(BenefitMutationError.NotFound);

        var now = DateTimeOffset.UtcNow;
        if (!offer.IsActive || now < offer.StartAt || now > offer.EndAt)
            return BenefitRedeemResult.Fail(BenefitMutationError.InvalidState);

        var userExists = await db.Users.AnyAsync(u => u.Id == userId, cancellationToken).ConfigureAwait(false);
        if (!userExists)
            return BenefitRedeemResult.Fail(BenefitMutationError.Validation);

        var planRows = await db.BenefitOfferPlanEligibilities.Where(x => x.OfferId == offerId).ToListAsync(cancellationToken).ConfigureAwait(false);
        var statusRows = await db.BenefitOfferMembershipStatusEligibilities.Where(x => x.OfferId == offerId).ToListAsync(cancellationToken).ConfigureAwait(false);
        var membership = await db.Memberships.FirstOrDefaultAsync(m => m.UserId == userId, cancellationToken).ConfigureAwait(false);
        var snapshot = membership is null
            ? null
            : new MembershipRecordSnapshot(membership.PlanId, membership.Status);

        if (!BenefitOfferEligibility.MatchesPlanAndStatus(
                planRows.Select(p => p.PlanId).ToList(),
                statusRows.Select(s => s.Status).ToList(),
                snapshot))
            return BenefitRedeemResult.Fail(BenefitMutationError.Validation);

        var redemptionId = Guid.NewGuid();
        db.BenefitRedemptions.Add(
            new BenefitRedemptionRecord
            {
                Id = redemptionId,
                OfferId = offerId,
                UserId = userId,
                ActorUserId = actorUserId,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                CreatedAt = now,
            });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return BenefitRedeemResult.Success(redemptionId);
    }

    public async Task<BenefitRedemptionListPageDto> ListRedemptionsAsync(
        Guid? offerId,
        Guid? userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query =
            from r in db.BenefitRedemptions.AsNoTracking()
            join o in db.BenefitOffers.AsNoTracking() on r.OfferId equals o.Id
            join u in db.Users.AsNoTracking() on r.UserId equals u.Id
            select new { r, o, u };

        if (offerId is { } oid)
            query = query.Where(x => x.r.OfferId == oid);
        if (userId is { } uid)
            query = query.Where(x => x.r.UserId == uid);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await query
            .OrderByDescending(x => x.r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = rows
            .Select(x => new BenefitRedemptionListItemDto(
                x.r.Id,
                x.r.OfferId,
                x.o.Title,
                x.r.UserId,
                x.u.Email ?? "",
                x.r.ActorUserId,
                x.r.Notes,
                x.r.CreatedAt))
            .ToList();

        return new BenefitRedemptionListPageDto(total, items);
    }

    private void ReplaceEligibilities(
        Guid offerId,
        IReadOnlyList<Guid>? eligiblePlanIds,
        IReadOnlyList<MembershipStatus>? eligibleMembershipStatuses)
    {
        if (eligiblePlanIds is { Count: > 0 })
        {
            foreach (var planId in eligiblePlanIds.Distinct())
            {
                db.BenefitOfferPlanEligibilities.Add(new BenefitOfferPlanEligibilityRecord { OfferId = offerId, PlanId = planId });
            }
        }

        if (eligibleMembershipStatuses is { Count: > 0 })
        {
            foreach (var st in eligibleMembershipStatuses.Distinct())
            {
                db.BenefitOfferMembershipStatusEligibilities.Add(
                    new BenefitOfferMembershipStatusEligibilityRecord { OfferId = offerId, Status = st });
            }
        }
    }
}
