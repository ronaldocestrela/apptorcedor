using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Support;

public sealed class SupportAdministrationService(AppDbContext db) : ISupportAdministrationPort
{
    public async Task<AdminSupportTicketListPageDto> ListTicketsAsync(
        string? queue,
        SupportTicketStatus? status,
        Guid? assignedUserId,
        bool? unassignedOnly,
        bool? slaBreachedOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.SupportTickets.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(queue))
        {
            var q = queue.Trim();
            query = query.Where(t => t.Queue == q);
        }

        if (status is { } st)
            query = query.Where(t => t.Status == st);

        if (assignedUserId is { } aid)
            query = query.Where(t => t.AssignedAgentUserId == aid);

        if (unassignedOnly == true)
            query = query.Where(t => t.AssignedAgentUserId == null);

        var now = DateTimeOffset.UtcNow;
        if (slaBreachedOnly == true)
        {
            query = query.Where(t =>
                now > t.SlaDeadlineUtc
                && t.Status != SupportTicketStatus.Resolved
                && t.Status != SupportTicketStatus.Closed);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await query
            .OrderByDescending(t => t.UpdatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = rows.Select(MapListItem).ToList();
        return new AdminSupportTicketListPageDto(total, items);
    }

    public async Task<AdminSupportTicketDetailDto?> GetTicketByIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var t = await db.SupportTickets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == ticketId, cancellationToken).ConfigureAwait(false);
        if (t is null)
            return null;

        var messages = await db.SupportTicketMessages.AsNoTracking()
            .Where(m => m.TicketId == ticketId)
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var history = await db.SupportTicketHistories.AsNoTracking()
            .Where(h => h.TicketId == ticketId)
            .OrderBy(h => h.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return MapDetail(t, messages, history);
    }

    public async Task<SupportTicketCreateResult> CreateTicketAsync(
        AdminSupportTicketCreateDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var err = ValidateCreate(dto);
        if (err is not null)
            return new SupportTicketCreateResult(null, err.Value);

        if (!await UserExistsAsync(dto.RequesterUserId, cancellationToken).ConfigureAwait(false))
            return new SupportTicketCreateResult(null, SupportTicketMutationError.Validation);

        if (!await UserExistsAsync(actorUserId, cancellationToken).ConfigureAwait(false))
            return new SupportTicketCreateResult(null, SupportTicketMutationError.Validation);

        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();
        var sla = ComputeSlaDeadline(dto.Priority, now);

        var row = new SupportTicketRecord
        {
            Id = id,
            RequesterUserId = dto.RequesterUserId,
            AssignedAgentUserId = null,
            Queue = dto.Queue.Trim(),
            Subject = dto.Subject.Trim(),
            Priority = dto.Priority,
            Status = SupportTicketStatus.Open,
            SlaDeadlineUtc = sla,
            FirstResponseAtUtc = null,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        db.SupportTickets.Add(row);
        db.SupportTicketHistories.Add(
            new SupportTicketHistoryRecord
            {
                Id = Guid.NewGuid(),
                TicketId = id,
                EventType = "Created",
                FromValue = null,
                ToValue = SupportTicketStatus.Open.ToString(),
                ActorUserId = actorUserId,
                Reason = null,
                CreatedAtUtc = now,
            });

        if (!string.IsNullOrWhiteSpace(dto.InitialMessage))
        {
            db.SupportTicketMessages.Add(
                new SupportTicketMessageRecord
                {
                    Id = Guid.NewGuid(),
                    TicketId = id,
                    AuthorUserId = actorUserId,
                    Body = dto.InitialMessage.Trim(),
                    IsInternal = false,
                    CreatedAtUtc = now,
                });
            db.SupportTicketHistories.Add(
                new SupportTicketHistoryRecord
                {
                    Id = Guid.NewGuid(),
                    TicketId = id,
                    EventType = "Reply",
                    FromValue = null,
                    ToValue = "initial",
                    ActorUserId = actorUserId,
                    Reason = null,
                    CreatedAtUtc = now,
                });
            row.FirstResponseAtUtc = now;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new SupportTicketCreateResult(id, null);
    }

    public async Task<SupportTicketMutationResult> ReplyTicketAsync(
        Guid ticketId,
        string body,
        bool isInternal,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(body))
            return new SupportTicketMutationResult(false, SupportTicketMutationError.Validation);

        var ticket = await db.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken).ConfigureAwait(false);
        if (ticket is null)
            return new SupportTicketMutationResult(false, SupportTicketMutationError.NotFound);

        if (ticket.Status == SupportTicketStatus.Closed)
            return new SupportTicketMutationResult(false, SupportTicketMutationError.Conflict);

        if (!await UserExistsAsync(actorUserId, cancellationToken).ConfigureAwait(false))
            return new SupportTicketMutationResult(false, SupportTicketMutationError.Validation);

        var now = DateTimeOffset.UtcNow;
        db.SupportTicketMessages.Add(
            new SupportTicketMessageRecord
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                AuthorUserId = actorUserId,
                Body = body.Trim(),
                IsInternal = isInternal,
                CreatedAtUtc = now,
            });

        db.SupportTicketHistories.Add(
            new SupportTicketHistoryRecord
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                EventType = "Reply",
                FromValue = null,
                ToValue = isInternal ? "internal" : "public",
                ActorUserId = actorUserId,
                Reason = null,
                CreatedAtUtc = now,
            });

        ticket.FirstResponseAtUtc ??= now;
        ticket.UpdatedAtUtc = now;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new SupportTicketMutationResult(true, null);
    }

    public async Task<SupportTicketMutationResult> AssignTicketAsync(
        Guid ticketId,
        Guid? agentUserId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var ticket = await db.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken).ConfigureAwait(false);
        if (ticket is null)
            return new SupportTicketMutationResult(false, SupportTicketMutationError.NotFound);

        if (ticket.Status == SupportTicketStatus.Closed)
            return new SupportTicketMutationResult(false, SupportTicketMutationError.Conflict);

        if (agentUserId is { } aid && !await UserExistsAsync(aid, cancellationToken).ConfigureAwait(false))
            return new SupportTicketMutationResult(false, SupportTicketMutationError.Validation);

        if (!await UserExistsAsync(actorUserId, cancellationToken).ConfigureAwait(false))
            return new SupportTicketMutationResult(false, SupportTicketMutationError.Validation);

        var now = DateTimeOffset.UtcNow;
        var from = ticket.AssignedAgentUserId?.ToString();
        ticket.AssignedAgentUserId = agentUserId;
        ticket.UpdatedAtUtc = now;

        db.SupportTicketHistories.Add(
            new SupportTicketHistoryRecord
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                EventType = "Assigned",
                FromValue = from,
                ToValue = agentUserId?.ToString(),
                ActorUserId = actorUserId,
                Reason = null,
                CreatedAtUtc = now,
            });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new SupportTicketMutationResult(true, null);
    }

    public async Task<SupportTicketMutationResult> ChangeStatusAsync(
        Guid ticketId,
        SupportTicketStatus newStatus,
        string? reason,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var ticket = await db.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken).ConfigureAwait(false);
        if (ticket is null)
            return new SupportTicketMutationResult(false, SupportTicketMutationError.NotFound);

        if (!await UserExistsAsync(actorUserId, cancellationToken).ConfigureAwait(false))
            return new SupportTicketMutationResult(false, SupportTicketMutationError.Validation);

        if (!IsValidTransition(ticket.Status, newStatus))
            return new SupportTicketMutationResult(false, SupportTicketMutationError.InvalidStatusTransition);

        var now = DateTimeOffset.UtcNow;
        var old = ticket.Status;
        ticket.Status = newStatus;
        ticket.UpdatedAtUtc = now;

        db.SupportTicketHistories.Add(
            new SupportTicketHistoryRecord
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                EventType = "StatusChanged",
                FromValue = old.ToString(),
                ToValue = newStatus.ToString(),
                ActorUserId = actorUserId,
                Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
                CreatedAtUtc = now,
            });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new SupportTicketMutationResult(true, null);
    }

    private static AdminSupportTicketListItemDto MapListItem(SupportTicketRecord t)
    {
        var now = DateTimeOffset.UtcNow;
        var breached = now > t.SlaDeadlineUtc
            && t.Status != SupportTicketStatus.Resolved
            && t.Status != SupportTicketStatus.Closed;
        return new AdminSupportTicketListItemDto(
            t.Id,
            t.RequesterUserId,
            t.AssignedAgentUserId,
            t.Queue,
            t.Subject,
            t.Priority,
            t.Status,
            t.SlaDeadlineUtc,
            breached,
            t.FirstResponseAtUtc,
            t.CreatedAtUtc,
            t.UpdatedAtUtc);
    }

    private static AdminSupportTicketDetailDto MapDetail(
        SupportTicketRecord t,
        IReadOnlyList<SupportTicketMessageRecord> messages,
        IReadOnlyList<SupportTicketHistoryRecord> history)
    {
        var now = DateTimeOffset.UtcNow;
        var breached = now > t.SlaDeadlineUtc
            && t.Status != SupportTicketStatus.Resolved
            && t.Status != SupportTicketStatus.Closed;

        var msgDtos = messages
            .Select(m => new SupportTicketMessageDto(m.Id, m.AuthorUserId, m.Body, m.IsInternal, m.CreatedAtUtc))
            .ToList();

        var histDtos = history
            .Select(h => new SupportTicketHistoryEntryDto(
                h.Id,
                h.EventType,
                h.FromValue,
                h.ToValue,
                h.ActorUserId,
                h.Reason,
                h.CreatedAtUtc))
            .ToList();

        return new AdminSupportTicketDetailDto(
            t.Id,
            t.RequesterUserId,
            t.AssignedAgentUserId,
            t.Queue,
            t.Subject,
            t.Priority,
            t.Status,
            t.SlaDeadlineUtc,
            breached,
            t.FirstResponseAtUtc,
            t.CreatedAtUtc,
            t.UpdatedAtUtc,
            msgDtos,
            histDtos);
    }

    private Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Users.AsNoTracking().AnyAsync(u => u.Id == userId, cancellationToken);

    private static SupportTicketMutationError? ValidateCreate(AdminSupportTicketCreateDto dto)
    {
        if (dto.RequesterUserId == Guid.Empty)
            return SupportTicketMutationError.Validation;
        if (string.IsNullOrWhiteSpace(dto.Queue) || dto.Queue.Trim().Length > 64)
            return SupportTicketMutationError.Validation;
        if (string.IsNullOrWhiteSpace(dto.Subject) || dto.Subject.Trim().Length > 500)
            return SupportTicketMutationError.Validation;
        return null;
    }

    private static DateTimeOffset ComputeSlaDeadline(SupportTicketPriority priority, DateTimeOffset now) =>
        priority switch
        {
            SupportTicketPriority.Urgent => now.AddHours(4),
            SupportTicketPriority.High => now.AddHours(24),
            _ => now.AddHours(48),
        };

    private static bool IsValidTransition(SupportTicketStatus from, SupportTicketStatus to)
    {
        if (from == to)
            return false;

        if (from == SupportTicketStatus.Closed)
            return to == SupportTicketStatus.Open;

        return (from, to) switch
        {
            (SupportTicketStatus.Open, SupportTicketStatus.InProgress) => true,
            (SupportTicketStatus.Open, SupportTicketStatus.WaitingUser) => true,
            (SupportTicketStatus.Open, SupportTicketStatus.Resolved) => true,
            (SupportTicketStatus.Open, SupportTicketStatus.Closed) => true,
            (SupportTicketStatus.InProgress, SupportTicketStatus.WaitingUser) => true,
            (SupportTicketStatus.InProgress, SupportTicketStatus.Resolved) => true,
            (SupportTicketStatus.InProgress, SupportTicketStatus.Open) => true,
            (SupportTicketStatus.InProgress, SupportTicketStatus.Closed) => true,
            (SupportTicketStatus.WaitingUser, SupportTicketStatus.InProgress) => true,
            (SupportTicketStatus.WaitingUser, SupportTicketStatus.Resolved) => true,
            (SupportTicketStatus.WaitingUser, SupportTicketStatus.Closed) => true,
            (SupportTicketStatus.Resolved, SupportTicketStatus.Closed) => true,
            (SupportTicketStatus.Resolved, SupportTicketStatus.InProgress) => true,
            (SupportTicketStatus.Resolved, SupportTicketStatus.Open) => true,
            _ => false,
        };
    }
}
