using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Tickets;

public sealed class TicketAdministrationService(
    AppDbContext db,
    ITicketProvider ticketProvider,
    ILoyaltyPointsTriggerPort loyaltyPoints) : ITicketAdministrationPort
{
    public async Task<AdminTicketListPageDto> ListTicketsAsync(
        Guid? userId,
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
            join u in db.Users.AsNoTracking() on t.UserId equals u.Id
            join g in db.Games.AsNoTracking() on t.GameId equals g.Id
            select new { t, u, g };

        if (userId is { } uid)
            query = query.Where(x => x.t.UserId == uid);
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
            .Select(x => new AdminTicketListItemDto(
                x.t.Id,
                x.t.UserId,
                x.u.Email ?? string.Empty,
                x.u.Name,
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

        return new AdminTicketListPageDto(total, items);
    }

    public async Task<AdminTicketDetailDto?> GetTicketByIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var row = await (
                from t in db.Tickets.AsNoTracking()
                join u in db.Users.AsNoTracking() on t.UserId equals u.Id
                join g in db.Games.AsNoTracking() on t.GameId equals g.Id
                where t.Id == ticketId
                select new { t, u, g })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (row is null)
            return null;

        return new AdminTicketDetailDto(
            row.t.Id,
            row.t.UserId,
            row.u.Email ?? string.Empty,
            row.u.Name,
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

    public async Task<TicketReserveResult> ReserveTicketAsync(
        Guid userId,
        Guid gameId,
        CancellationToken cancellationToken = default)
    {
        var game = await db.Games.FirstOrDefaultAsync(g => g.Id == gameId, cancellationToken).ConfigureAwait(false);
        if (game is null)
            return new TicketReserveResult(null, TicketMutationError.GameNotFound);
        if (!game.IsActive)
            return new TicketReserveResult(null, TicketMutationError.GameInactive);

        var userExists = await db.Users.AnyAsync(u => u.Id == userId, cancellationToken).ConfigureAwait(false);
        if (!userExists)
            return new TicketReserveResult(null, TicketMutationError.UserNotFound);

        var ticketId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var row = new TicketRecord
        {
            Id = ticketId,
            UserId = userId,
            GameId = gameId,
            Status = TicketStatus.Reserved,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Tickets.Add(row);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var res = await ticketProvider.ReserveAsync(ticketId, gameId, userId, cancellationToken).ConfigureAwait(false);
            row.ExternalTicketId = res.ExternalTicketId;
            row.QrCode = res.QrCodePayload;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            db.Tickets.Remove(row);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new TicketReserveResult(null, TicketMutationError.ProviderError);
        }

        return new TicketReserveResult(ticketId, null);
    }

    public async Task<TicketMutationResult> PurchaseTicketAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var row = await db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken).ConfigureAwait(false);
        if (row is null)
            return TicketMutationResult.Fail(TicketMutationError.NotFound);
        if (row.Status != TicketStatus.Reserved)
            return TicketMutationResult.Fail(TicketMutationError.InvalidTransition);
        if (string.IsNullOrWhiteSpace(row.ExternalTicketId))
            return TicketMutationResult.Fail(TicketMutationError.ExternalIdMissing);

        try
        {
            var r = await ticketProvider.PurchaseAsync(row.ExternalTicketId, cancellationToken).ConfigureAwait(false);
            row.Status = TicketStatus.Purchased;
            row.QrCode = r.QrCodePayload;
            row.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return TicketMutationResult.Fail(TicketMutationError.ProviderError);
        }

        await loyaltyPoints.AwardPointsForTicketPurchasedAsync(ticketId, cancellationToken).ConfigureAwait(false);
        return TicketMutationResult.Success();
    }

    public async Task<TicketMutationResult> SyncTicketAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var row = await db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken).ConfigureAwait(false);
        if (row is null)
            return TicketMutationResult.Fail(TicketMutationError.NotFound);
        if (row.Status == TicketStatus.Redeemed)
            return TicketMutationResult.Success();
        if (string.IsNullOrWhiteSpace(row.ExternalTicketId))
            return TicketMutationResult.Fail(TicketMutationError.ExternalIdMissing);

        try
        {
            var previousStatus = row.Status;
            var snap = await ticketProvider.GetAsync(row.ExternalTicketId, cancellationToken).ConfigureAwait(false);
            row.QrCode = snap.QrCodePayload;
            if (row.Status == TicketStatus.Reserved
                && string.Equals(snap.ProviderStatus, "Purchased", StringComparison.OrdinalIgnoreCase))
                row.Status = TicketStatus.Purchased;

            row.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            if (previousStatus == TicketStatus.Reserved && row.Status == TicketStatus.Purchased)
                await loyaltyPoints.AwardPointsForTicketPurchasedAsync(ticketId, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return TicketMutationResult.Fail(TicketMutationError.ProviderError);
        }

        return TicketMutationResult.Success();
    }

    public async Task<TicketMutationResult> RedeemTicketAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var row = await db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken).ConfigureAwait(false);
        if (row is null)
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
