using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Loyalty;

public sealed class LoyaltyAdministrationService(AppDbContext db) : ILoyaltyAdministrationPort, ILoyaltyPointsTriggerPort
{
    public async Task<LoyaltyCampaignListPageDto> ListCampaignsAsync(
        LoyaltyCampaignStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.LoyaltyCampaigns.AsNoTracking();
        if (status is { } s)
            query = query.Where(c => c.Status == s);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await query
            .OrderByDescending(c => c.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var ids = rows.Select(r => r.Id).ToList();
        var ruleCounts = await db.LoyaltyPointRules.AsNoTracking()
            .Where(r => ids.Contains(r.CampaignId))
            .GroupBy(r => r.CampaignId)
            .Select(g => new { CampaignId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CampaignId, x => x.Count, cancellationToken)
            .ConfigureAwait(false);

        var items = rows
            .Select(c => new LoyaltyCampaignListItemDto(
                c.Id,
                c.Name,
                c.Status,
                c.CreatedAt,
                c.UpdatedAt,
                c.PublishedAt,
                ruleCounts.GetValueOrDefault(c.Id)))
            .ToList();

        return new LoyaltyCampaignListPageDto(total, items);
    }

    public async Task<LoyaltyCampaignDetailDto?> GetCampaignByIdAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        var c = await db.LoyaltyCampaigns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == campaignId, cancellationToken).ConfigureAwait(false);
        if (c is null)
            return null;

        var rules = await db.LoyaltyPointRules.AsNoTracking()
            .Where(r => r.CampaignId == campaignId)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Id)
            .Select(r => new LoyaltyPointRuleDto(r.Id, r.Trigger, r.Points, r.SortOrder))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new LoyaltyCampaignDetailDto(
            c.Id,
            c.Name,
            c.Description,
            c.Status,
            c.CreatedAt,
            c.UpdatedAt,
            c.PublishedAt,
            c.UnpublishedAt,
            rules);
    }

    public async Task<LoyaltyCampaignCreateResult> CreateCampaignAsync(LoyaltyCampaignWriteDto dto, CancellationToken cancellationToken = default)
    {
        if (!ValidateWrite(dto, requireRules: false, out _))
            return new LoyaltyCampaignCreateResult(null, LoyaltyMutationError.Validation);

        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();
        db.LoyaltyCampaigns.Add(
            new LoyaltyCampaignRecord
            {
                Id = id,
                Name = dto.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                Status = LoyaltyCampaignStatus.Draft,
                CreatedAt = now,
                UpdatedAt = now,
            });

        AddRules(id, dto.Rules, now);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new LoyaltyCampaignCreateResult(id, null);
    }

    public async Task<LoyaltyMutationResult> UpdateCampaignAsync(
        Guid campaignId,
        LoyaltyCampaignWriteDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ValidateWrite(dto, requireRules: false, out _))
            return LoyaltyMutationResult.Fail(LoyaltyMutationError.Validation);

        var c = await db.LoyaltyCampaigns.FirstOrDefaultAsync(x => x.Id == campaignId, cancellationToken).ConfigureAwait(false);
        if (c is null)
            return LoyaltyMutationResult.Fail(LoyaltyMutationError.NotFound);
        if (c.Status == LoyaltyCampaignStatus.Published)
            return LoyaltyMutationResult.Fail(LoyaltyMutationError.InvalidState);

        var now = DateTimeOffset.UtcNow;
        c.Name = dto.Name.Trim();
        c.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        c.UpdatedAt = now;

        var existing = await db.LoyaltyPointRules.Where(r => r.CampaignId == campaignId).ToListAsync(cancellationToken).ConfigureAwait(false);
        db.LoyaltyPointRules.RemoveRange(existing);
        AddRules(campaignId, dto.Rules, now);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return LoyaltyMutationResult.Success();
    }

    public async Task<LoyaltyMutationResult> PublishCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        var c = await db.LoyaltyCampaigns.FirstOrDefaultAsync(x => x.Id == campaignId, cancellationToken).ConfigureAwait(false);
        if (c is null)
            return LoyaltyMutationResult.Fail(LoyaltyMutationError.NotFound);
        if (c.Status == LoyaltyCampaignStatus.Published)
            return LoyaltyMutationResult.Success();

        var rules = await db.LoyaltyPointRules.Where(r => r.CampaignId == campaignId).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (rules.Count == 0 || rules.All(r => r.Points == 0))
            return LoyaltyMutationResult.Fail(LoyaltyMutationError.Validation);

        var now = DateTimeOffset.UtcNow;
        c.Status = LoyaltyCampaignStatus.Published;
        c.PublishedAt = now;
        c.UnpublishedAt = null;
        c.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return LoyaltyMutationResult.Success();
    }

    public async Task<LoyaltyMutationResult> UnpublishCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        var c = await db.LoyaltyCampaigns.FirstOrDefaultAsync(x => x.Id == campaignId, cancellationToken).ConfigureAwait(false);
        if (c is null)
            return LoyaltyMutationResult.Fail(LoyaltyMutationError.NotFound);
        if (c.Status != LoyaltyCampaignStatus.Published)
            return LoyaltyMutationResult.Fail(LoyaltyMutationError.InvalidState);

        var now = DateTimeOffset.UtcNow;
        c.Status = LoyaltyCampaignStatus.Unpublished;
        c.UnpublishedAt = now;
        c.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return LoyaltyMutationResult.Success();
    }

    public async Task<LoyaltyManualAdjustResult> ManualAdjustAsync(
        Guid userId,
        int points,
        string reason,
        Guid? campaignId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (points == 0 || string.IsNullOrWhiteSpace(reason))
            return LoyaltyManualAdjustResult.Fail(LoyaltyManualAdjustError.Validation);

        var userExists = await db.Users.AnyAsync(u => u.Id == userId, cancellationToken).ConfigureAwait(false);
        if (!userExists)
            return LoyaltyManualAdjustResult.Fail(LoyaltyManualAdjustError.NotFound);

        if (campaignId is { } cid
            && !await db.LoyaltyCampaigns.AnyAsync(c => c.Id == cid, cancellationToken).ConfigureAwait(false))
            return LoyaltyManualAdjustResult.Fail(LoyaltyManualAdjustError.Validation);

        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        db.LoyaltyPointLedgerEntries.Add(
            new LoyaltyPointLedgerEntryRecord
            {
                Id = id,
                UserId = userId,
                CampaignId = campaignId,
                RuleId = null,
                Points = points,
                SourceType = LoyaltyPointSourceType.Manual,
                SourceKey = id.ToString("N"),
                Reason = reason.Trim(),
                ActorUserId = actorUserId,
                CreatedAt = now,
            });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return LoyaltyManualAdjustResult.Success();
    }

    public async Task<LoyaltyLedgerPageDto> ListUserLedgerAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.LoyaltyPointLedgerEntries.AsNoTracking().Where(e => e.UserId == userId);
        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = rows
            .Select(e => new LoyaltyLedgerEntryDto(
                e.Id,
                e.UserId,
                e.CampaignId,
                e.RuleId,
                e.Points,
                e.SourceType,
                e.SourceKey,
                e.Reason,
                e.ActorUserId,
                e.CreatedAt))
            .ToList();

        return new LoyaltyLedgerPageDto(total, items);
    }

    public async Task<LoyaltyRankingPageDto> GetMonthlyRankingAsync(
        int year,
        int month,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (month is < 1 or > 12)
            return new LoyaltyRankingPageDto(0, []);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var from = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddMonths(1);

        return await BuildRankingAsync(e => e.CreatedAt >= from && e.CreatedAt < to, page, pageSize, cancellationToken).ConfigureAwait(false);
    }

    public async Task<LoyaltyRankingPageDto> GetAllTimeRankingAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        await BuildRankingAsync(_ => true, page, pageSize, cancellationToken).ConfigureAwait(false);

    public Task AwardPointsForPaymentPaidAsync(Guid paymentId, CancellationToken cancellationToken = default) =>
        AwardForTriggerAsync(
            paymentId,
            isPayment: true,
            LoyaltyPointRuleTrigger.PaymentPaid,
            LoyaltyPointSourceType.Payment,
            cancellationToken);

    public Task AwardPointsForTicketPurchasedAsync(Guid ticketId, CancellationToken cancellationToken = default) =>
        AwardForTriggerAsync(
            ticketId,
            isPayment: false,
            LoyaltyPointRuleTrigger.TicketPurchased,
            LoyaltyPointSourceType.TicketPurchase,
            cancellationToken);

    public Task AwardPointsForTicketRedeemedAsync(Guid ticketId, CancellationToken cancellationToken = default) =>
        AwardForTriggerAsync(
            ticketId,
            isPayment: false,
            LoyaltyPointRuleTrigger.TicketRedeemed,
            LoyaltyPointSourceType.TicketRedeem,
            cancellationToken);

    private async Task AwardForTriggerAsync(
        Guid entityId,
        bool isPayment,
        LoyaltyPointRuleTrigger trigger,
        LoyaltyPointSourceType sourceType,
        CancellationToken cancellationToken)
    {
        Guid userId;
        if (isPayment)
        {
            var payment = await db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == entityId, cancellationToken).ConfigureAwait(false);
            if (payment is null)
                return;
            userId = payment.UserId;
        }
        else
        {
            var ticket = await db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == entityId, cancellationToken).ConfigureAwait(false);
            if (ticket is null)
                return;
            userId = ticket.UserId;
        }

        var entityKey = entityId.ToString("N");
        var campaignIds = await db.LoyaltyCampaigns.AsNoTracking()
            .Where(c => c.Status == LoyaltyCampaignStatus.Published)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (campaignIds.Count == 0)
            return;

        var rules = await db.LoyaltyPointRules.AsNoTracking()
            .Where(r => campaignIds.Contains(r.CampaignId) && r.Trigger == trigger && r.Points != 0)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (rules.Count == 0)
            return;

        var now = DateTimeOffset.UtcNow;
        foreach (var rule in rules)
        {
            var sourceKey = $"{entityKey}|{rule.Id:N}";
            var exists = await db.LoyaltyPointLedgerEntries.AnyAsync(
                    e => e.SourceType == sourceType && e.SourceKey == sourceKey,
                    cancellationToken)
                .ConfigureAwait(false);
            if (exists)
                continue;

            db.LoyaltyPointLedgerEntries.Add(
                new LoyaltyPointLedgerEntryRecord
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CampaignId = rule.CampaignId,
                    RuleId = rule.Id,
                    Points = rule.Points,
                    SourceType = sourceType,
                    SourceKey = sourceKey,
                    Reason = null,
                    ActorUserId = null,
                    CreatedAt = now,
                });
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<LoyaltyRankingPageDto> BuildRankingAsync(
        System.Linq.Expressions.Expression<Func<LoyaltyPointLedgerEntryRecord, bool>> predicate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var grouped = await db.LoyaltyPointLedgerEntries.AsNoTracking()
            .Where(predicate)
            .GroupBy(e => e.UserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.Points) })
            .Where(x => x.Total != 0)
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.UserId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var totalCount = grouped.Count;
        var slice = grouped.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        if (slice.Count == 0)
            return new LoyaltyRankingPageDto(totalCount, []);

        var userIds = slice.Select(s => s.UserId).ToList();
        var users = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken)
            .ConfigureAwait(false);

        var rankBase = (page - 1) * pageSize;
        var rows = new List<LoyaltyRankingRowDto>();
        for (var i = 0; i < slice.Count; i++)
        {
            var s = slice[i];
            users.TryGetValue(s.UserId, out var u);
            rows.Add(
                new LoyaltyRankingRowDto(
                    rankBase + i + 1,
                    s.UserId,
                    u?.Email ?? "",
                    u?.Name ?? "",
                    s.Total));
        }

        return new LoyaltyRankingPageDto(totalCount, rows);
    }

    private static bool ValidateWrite(LoyaltyCampaignWriteDto dto, bool requireRules, out string? _)
    {
        _ = null;
        if (string.IsNullOrWhiteSpace(dto.Name))
            return false;
        if (requireRules && dto.Rules.Count == 0)
            return false;
        foreach (var r in dto.Rules)
        {
            if (r.Points == 0)
                return false;
        }

        return true;
    }

    private void AddRules(Guid campaignId, IReadOnlyList<LoyaltyPointRuleWriteDto> rules, DateTimeOffset now)
    {
        foreach (var r in rules.OrderBy(x => x.SortOrder))
        {
            db.LoyaltyPointRules.Add(
                new LoyaltyPointRuleRecord
                {
                    Id = Guid.NewGuid(),
                    CampaignId = campaignId,
                    Trigger = r.Trigger,
                    Points = r.Points,
                    SortOrder = r.SortOrder,
                });
        }
    }
}
