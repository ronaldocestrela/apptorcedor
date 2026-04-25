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
        string? requestStatus,
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

        TicketRequestStatus? requestStatusFilter = null;
        if (!string.IsNullOrWhiteSpace(requestStatus)
            && Enum.TryParse<TicketRequestStatus>(requestStatus, ignoreCase: true, out var parsedRequest))
            requestStatusFilter = parsedRequest;
        if (requestStatusFilter is { } rqf)
            query = query.Where(x => x.t.RequestStatus == rqf);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await query
            .OrderByDescending(x => x.t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var userIds = rows.Select(x => x.t.UserId).Distinct().ToList();
        var planByUser = await GetLatestPlanNamesByUserIdsAsync(userIds, cancellationToken).ConfigureAwait(false);

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
                x.t.RedeemedAt,
                x.t.RequestStatus.ToString(),
                planByUser.GetValueOrDefault(x.t.UserId)))
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

        var planByUser = await GetLatestPlanNamesByUserIdsAsync(
                new[] { row.t.UserId },
                cancellationToken)
            .ConfigureAwait(false);
        var planName = planByUser.GetValueOrDefault(row.t.UserId);

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
            row.t.RedeemedAt,
            row.t.RequestStatus.ToString(),
            planName);
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

        if (await db.Tickets.AnyAsync(t => t.UserId == userId && t.GameId == gameId, cancellationToken)
                .ConfigureAwait(false))
            return new TicketReserveResult(null, TicketMutationError.TicketAlreadyExistsForGame);

        var ticketId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var row = new TicketRecord
        {
            Id = ticketId,
            UserId = userId,
            GameId = gameId,
            Status = TicketStatus.Reserved,
            RequestStatus = TicketRequestStatus.Pending,
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

    public async Task<TicketMutationResult> UpdateTicketRequestStatusAsync(
        Guid ticketId,
        string requestStatus,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(requestStatus)
            || !Enum.TryParse<TicketRequestStatus>(requestStatus, ignoreCase: true, out var newStatus))
            return TicketMutationResult.Fail(TicketMutationError.InvalidRequestStatus);

        var row = await db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken).ConfigureAwait(false);
        if (row is null)
            return TicketMutationResult.Fail(TicketMutationError.NotFound);

        row.RequestStatus = newStatus;
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return TicketMutationResult.Success();
    }

    private async Task<Dictionary<Guid, string?>> GetLatestPlanNamesByUserIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
            return new Dictionary<Guid, string?>();

        var pairs = await (
                from m in db.Memberships.AsNoTracking()
                join p in db.MembershipPlans.AsNoTracking() on m.PlanId equals p.Id
                where userIds.Contains(m.UserId) && m.PlanId != null
                select new { m.UserId, m.StartDate, p.Name }
            )
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return pairs
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => (string?)g.OrderByDescending(x => x.StartDate).First().Name);
    }
}
