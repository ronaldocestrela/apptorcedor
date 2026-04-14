using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Governance;

public sealed class MembershipAdministrationService(AppDbContext db) : IMembershipAdministrationPort
{
    public async Task<AdminMembershipListPageDto> ListMembershipsAsync(
        MembershipStatus? status,
        Guid? userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query =
            from m in db.Memberships.AsNoTracking()
            join u in db.Users.AsNoTracking() on m.UserId equals u.Id
            select new { m, u };

        if (status is { } s)
            query = query.Where(x => x.m.Status == s);
        if (userId is { } uid)
            query = query.Where(x => x.m.UserId == uid);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var pageQuery = query
            .OrderBy(x => x.u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var rows = await pageQuery.ToListAsync(cancellationToken).ConfigureAwait(false);
        var items = rows.Select(x => new AdminMembershipListItemDto(
                x.m.Id,
                x.m.UserId,
                x.u.Email ?? string.Empty,
                x.u.Name,
                x.m.Status.ToString(),
                x.m.PlanId,
                x.m.StartDate,
                x.m.EndDate,
                x.m.NextDueDate))
            .ToList();

        return new AdminMembershipListPageDto(total, items);
    }

    public async Task<AdminMembershipDetailDto?> GetMembershipByIdAsync(Guid membershipId, CancellationToken cancellationToken = default)
    {
        var row = await (
                from m in db.Memberships.AsNoTracking()
                join u in db.Users.AsNoTracking() on m.UserId equals u.Id
                where m.Id == membershipId
                select new { m, u })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (row is null)
            return null;

        return new AdminMembershipDetailDto(
            row.m.Id,
            row.m.UserId,
            row.u.Email ?? string.Empty,
            row.u.Name,
            row.m.Status.ToString(),
            row.m.PlanId,
            row.m.StartDate,
            row.m.EndDate,
            row.m.NextDueDate);
    }

    public async Task<IReadOnlyList<MembershipHistoryEventDto>> ListHistoryAsync(
        Guid membershipId,
        int take,
        CancellationToken cancellationToken = default)
    {
               take = Math.Clamp(take, 1, 200);

        var rows = await db.MembershipHistories.AsNoTracking()
            .Where(h => h.MembershipId == membershipId)
            .OrderByDescending(h => h.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(h => new MembershipHistoryEventDto(
                h.Id,
                h.EventType,
                h.FromStatus?.ToString(),
                h.ToStatus.ToString(),
                h.FromPlanId,
                h.ToPlanId,
                h.Reason,
                h.ActorUserId,
                h.CreatedAt))
            .ToList();
    }

    public async Task<MembershipStatusUpdateResult> UpdateStatusAsync(
        Guid membershipId,
        MembershipStatus status,
        string reason,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var membership = await db.Memberships.FirstOrDefaultAsync(m => m.Id == membershipId, cancellationToken).ConfigureAwait(false);
        if (membership is null)
            return new MembershipStatusUpdateResult(false, MembershipStatusUpdateError.NotFound);
        if (membership.Status == status)
            return new MembershipStatusUpdateResult(false, MembershipStatusUpdateError.Unchanged);

        var utc = DateTimeOffset.UtcNow;
        var fromStatus = membership.Status;
        var planId = membership.PlanId;

        membership.Status = status;

        db.MembershipHistories.Add(
            new MembershipHistoryRecord
            {
                Id = Guid.NewGuid(),
                MembershipId = membership.Id,
                UserId = membership.UserId,
                EventType = MembershipHistoryEventTypes.StatusChanged,
                FromStatus = fromStatus,
                ToStatus = status,
                FromPlanId = planId,
                ToPlanId = planId,
                Reason = reason,
                ActorUserId = actorUserId,
                CreatedAt = utc,
            });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new MembershipStatusUpdateResult(true, null);
    }

    public async Task<MembershipStatusUpdateResult> ApplySystemMembershipTransitionAsync(
        Guid membershipId,
        MembershipStatus toStatus,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var membership = await db.Memberships.FirstOrDefaultAsync(m => m.Id == membershipId, cancellationToken).ConfigureAwait(false);
        if (membership is null)
            return new MembershipStatusUpdateResult(false, MembershipStatusUpdateError.NotFound);
        if (membership.Status == toStatus)
            return new MembershipStatusUpdateResult(false, MembershipStatusUpdateError.Unchanged);

        var from = membership.Status;
        if (toStatus == MembershipStatus.Inadimplente)
        {
            if (from != MembershipStatus.Ativo)
                return new MembershipStatusUpdateResult(false, MembershipStatusUpdateError.InvalidTransition);
        }
        else if (toStatus == MembershipStatus.Ativo)
        {
            if (from != MembershipStatus.Inadimplente)
                return new MembershipStatusUpdateResult(false, MembershipStatusUpdateError.InvalidTransition);
        }
        else
            return new MembershipStatusUpdateResult(false, MembershipStatusUpdateError.InvalidTransition);

        if (from is MembershipStatus.Cancelado or MembershipStatus.Suspenso)
            return new MembershipStatusUpdateResult(false, MembershipStatusUpdateError.InvalidTransition);

        var utc = DateTimeOffset.UtcNow;
        var planId = membership.PlanId;
        membership.Status = toStatus;

        db.MembershipHistories.Add(
            new MembershipHistoryRecord
            {
                Id = Guid.NewGuid(),
                MembershipId = membership.Id,
                UserId = membership.UserId,
                EventType = MembershipHistoryEventTypes.StatusChanged,
                FromStatus = from,
                ToStatus = toStatus,
                FromPlanId = planId,
                ToPlanId = planId,
                Reason = reason,
                ActorUserId = null,
                CreatedAt = utc,
            });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new MembershipStatusUpdateResult(true, null);
    }
}
