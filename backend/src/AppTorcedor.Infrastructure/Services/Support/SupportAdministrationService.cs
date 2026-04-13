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

        var messageIds = messages.Select(m => m.Id).ToList();
        var attachments = messageIds.Count == 0
            ? []
            : await db.SupportTicketMessageAttachments.AsNoTracking()
                .Where(a => messageIds.Contains(a.MessageId))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        var attachmentsByMessage = attachments.GroupBy(a => a.MessageId).ToDictionary(g => g.Key, g => g.ToList());

        var history = await db.SupportTicketHistories.AsNoTracking()
            .Where(h => h.TicketId == ticketId)
            .OrderBy(h => h.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return MapDetail(t, messages, attachmentsByMessage, history);
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
        var sla = SupportTicketStateMachine.ComputeSlaDeadline(dto.Priority, now);

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

        if (!SupportTicketStateMachine.IsValidTransition(ticket.Status, newStatus))
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

    public async Task<SupportAttachmentDownloadDto?> GetSupportAttachmentDownloadAsync(
        Guid ticketId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var a = await db.SupportTicketMessageAttachments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == attachmentId, cancellationToken)
            .ConfigureAwait(false);
        if (a is null)
            return null;

        var message = await db.SupportTicketMessages.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == a.MessageId, cancellationToken)
            .ConfigureAwait(false);
        if (message is null || message.TicketId != ticketId)
            return null;

        var ticket = await db.SupportTickets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == ticketId, cancellationToken)
            .ConfigureAwait(false);
        if (ticket is null)
            return null;

        return new SupportAttachmentDownloadDto(
            a.Id,
            ticket.Id,
            ticket.RequesterUserId,
            a.OriginalFileName,
            a.ContentType,
            a.StorageKey);
    }

    private static AdminSupportTicketDetailDto MapDetail(
        SupportTicketRecord t,
        IReadOnlyList<SupportTicketMessageRecord> messages,
        IReadOnlyDictionary<Guid, List<SupportTicketMessageAttachmentRecord>> attachmentsByMessage,
        IReadOnlyList<SupportTicketHistoryRecord> history)
    {
        var now = DateTimeOffset.UtcNow;
        var breached = now > t.SlaDeadlineUtc
            && t.Status != SupportTicketStatus.Resolved
            && t.Status != SupportTicketStatus.Closed;

        var msgDtos = messages
            .Select(m =>
            {
                var atts = (attachmentsByMessage.TryGetValue(m.Id, out var list) ? list : [])
                    .Select(
                        att => new SupportTicketMessageAttachmentDto(
                            att.Id,
                            att.OriginalFileName,
                            att.ContentType,
                            $"/api/admin/support/tickets/{t.Id}/attachments/{att.Id}"))
                    .ToList();
                return new SupportTicketMessageDto(m.Id, m.AuthorUserId, m.Body, m.IsInternal, m.CreatedAtUtc, atts);
            })
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

}
