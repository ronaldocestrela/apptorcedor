using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Tickets;

public sealed class TicketTorcedorService(AppDbContext db, ILoyaltyPointsTriggerPort loyaltyPoints) : ITicketTorcedorPort
{
    public async Task<TorcedorTicketListPageDto> ListMyTicketsAsync(
        Guid userId,
        Guid? gameId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        TicketStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<TicketStatus>(status, ignoreCase: true, out var parsed))
            statusFilter = parsed;

        var query =
            from t in db.Tickets.AsNoTracking()
            join g in db.Games.AsNoTracking() on t.GameId equals g.Id
            where t.UserId == userId
            select new { t, g };

        if (gameId is { } gid)
            query = query.Where(x => x.t.GameId == gid);
        if (statusFilter is { } sf)
            query = query.Where(x => x.t.Status == sf);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await query
            .OrderByDescending(x => x.t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = rows
            .Select(x => new TorcedorTicketListItemDto(
                x.t.Id,
                x.t.GameId,
                x.g.Opponent,
                x.g.Competition,
                x.g.GameDate,
                x.t.Status.ToString(),
                x.t.ExternalTicketId,
                x.t.QrCode,
                x.t.CreatedAt,
                x.t.RedeemedAt))
            .ToList();

        return new TorcedorTicketListPageDto(total, items);
    }

    public async Task<TorcedorTicketDetailDto?> GetMyTicketAsync(Guid userId, Guid ticketId, CancellationToken cancellationToken = default)
    {
        var row = await (
                from t in db.Tickets.AsNoTracking()
                join g in db.Games.AsNoTracking() on t.GameId equals g.Id
                where t.Id == ticketId && t.UserId == userId
                select new { t, g })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (row is null)
            return null;

        return new TorcedorTicketDetailDto(
            row.t.Id,
            row.t.GameId,
            row.g.Opponent,
            row.g.Competition,
            row.g.GameDate,
            row.t.Status.ToString(),
            row.t.ExternalTicketId,
            row.t.QrCode,
            row.t.CreatedAt,
            row.t.UpdatedAt,
            row.t.RedeemedAt);
    }

    public async Task<TicketMutationResult> RedeemMyTicketAsync(Guid userId, Guid ticketId, CancellationToken cancellationToken = default)
    {
        var row = await db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken).ConfigureAwait(false);
        if (row is null || row.UserId != userId)
            return TicketMutationResult.Fail(TicketMutationError.NotFound);
        if (row.Status != TicketStatus.Purchased)
            return TicketMutationResult.Fail(TicketMutationError.InvalidTransition);

        var now = DateTimeOffset.UtcNow;
        row.Status = TicketStatus.Redeemed;
        row.RedeemedAt = now;
        row.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await loyaltyPoints.AwardPointsForTicketRedeemedAsync(ticketId, cancellationToken).ConfigureAwait(false);
        return TicketMutationResult.Success();
    }
}
