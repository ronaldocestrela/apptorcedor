using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AppTorcedor.Infrastructure.Services.Loyalty;

public sealed class LoyaltyTorcedorReadService(AppDbContext db) : ILoyaltyTorcedorReadPort
{
    public async Task<LoyaltyTorcedorSummaryDto> GetMySummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var asOf = DateTimeOffset.UtcNow;
        var from = new DateTimeOffset(asOf.Year, asOf.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddMonths(1);

        var totalPoints = await db.LoyaltyPointLedgerEntries.AsNoTracking()
            .Where(e => e.UserId == userId)
            .SumAsync(e => (int?)e.Points, cancellationToken)
            .ConfigureAwait(false) ?? 0;

        var monthlyPoints = await db.LoyaltyPointLedgerEntries.AsNoTracking()
            .Where(e => e.UserId == userId && e.CreatedAt >= from && e.CreatedAt < to)
            .SumAsync(e => (int?)e.Points, cancellationToken)
            .ConfigureAwait(false) ?? 0;

        var monthlyRank = await ComputeRankAsync(userId, e => e.CreatedAt >= from && e.CreatedAt < to, cancellationToken)
            .ConfigureAwait(false);
        var allTimeRank = await ComputeRankAsync(userId, _ => true, cancellationToken).ConfigureAwait(false);

        return new LoyaltyTorcedorSummaryDto(totalPoints, monthlyPoints, monthlyRank, allTimeRank, asOf);
    }

    public Task<LoyaltyTorcedorRankingPageDto> GetMonthlyRankingAsync(
        Guid currentUserId,
        int year,
        int month,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (month is < 1 or > 12)
            return Task.FromResult(new LoyaltyTorcedorRankingPageDto(0, [], null));

        var from = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddMonths(1);
        return BuildTorcedorRankingPageAsync(currentUserId, e => e.CreatedAt >= from && e.CreatedAt < to, page, pageSize, cancellationToken);
    }

    public Task<LoyaltyTorcedorRankingPageDto> GetAllTimeRankingAsync(
        Guid currentUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        BuildTorcedorRankingPageAsync(currentUserId, _ => true, page, pageSize, cancellationToken);

    private async Task<int?> ComputeRankAsync(
        Guid userId,
        Expression<Func<LoyaltyPointLedgerEntryRecord, bool>> predicate,
        CancellationToken cancellationToken)
    {
        var grouped = await db.LoyaltyPointLedgerEntries.AsNoTracking()
            .Where(predicate)
            .GroupBy(e => e.UserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.Points) })
            .Where(x => x.Total != 0)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var ordered = grouped
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.UserId)
            .ToList();

        var idx = ordered.FindIndex(x => x.UserId == userId);
        return idx < 0 ? null : idx + 1;
    }

    private async Task<LoyaltyTorcedorRankingPageDto> BuildTorcedorRankingPageAsync(
        Guid currentUserId,
        Expression<Func<LoyaltyPointLedgerEntryRecord, bool>> predicate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var grouped = await db.LoyaltyPointLedgerEntries.AsNoTracking()
            .Where(predicate)
            .GroupBy(e => e.UserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.Points) })
            .Where(x => x.Total != 0)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var ordered = grouped
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.UserId)
            .ToList();

        var totalCount = ordered.Count;

        LoyaltyTorcedorMyStandingDto? me = null;
        var userIndex = ordered.FindIndex(x => x.UserId == currentUserId);
        if (userIndex >= 0)
        {
            var row = ordered[userIndex];
            var u = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == currentUserId, cancellationToken)
                .ConfigureAwait(false);
            me = new LoyaltyTorcedorMyStandingDto(userIndex + 1, row.UserId, u?.Name ?? "", row.Total);
        }

        var slice = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        if (slice.Count == 0)
            return new LoyaltyTorcedorRankingPageDto(totalCount, [], me);

        var userIds = slice.Select(s => s.UserId).ToList();
        var users = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken)
            .ConfigureAwait(false);

        var rankBase = (page - 1) * pageSize;
        var items = new List<LoyaltyTorcedorRankingRowDto>();
        for (var i = 0; i < slice.Count; i++)
        {
            var s = slice[i];
            users.TryGetValue(s.UserId, out var user);
            items.Add(
                new LoyaltyTorcedorRankingRowDto(rankBase + i + 1, s.UserId, user?.Name ?? "", s.Total));
        }

        return new LoyaltyTorcedorRankingPageDto(totalCount, items, me);
    }
}
