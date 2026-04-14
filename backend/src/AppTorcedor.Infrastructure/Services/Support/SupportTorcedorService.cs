using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Options;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace AppTorcedor.Infrastructure.Services.Support;

public sealed class SupportTorcedorService(
    AppDbContext db,
    ISupportTicketAttachmentStorage attachmentStorage,
    IOptions<SupportTicketAttachmentStorageOptions> attachmentOptions) : ISupportTorcedorPort
{
    private const string AttachmentOnlyBodyPlaceholder = "(Arquivo anexo)";

    public async Task<TorcedorSupportTicketListPageDto> ListMyTicketsAsync(
        Guid requesterUserId,
        SupportTicketStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.SupportTickets.AsNoTracking().Where(t => t.RequesterUserId == requesterUserId);
        if (status is { } st)
            query = query.Where(t => t.Status == st);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await query
            .OrderByDescending(t => t.UpdatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = rows.Select(MapListItem).ToList();
        return new TorcedorSupportTicketListPageDto(total, items);
    }

    public async Task<TorcedorSupportTicketDetailDto?> GetMyTicketAsync(
        Guid ticketId,
        Guid requesterUserId,
        CancellationToken cancellationToken = default)
    {
        var t = await db.SupportTickets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == ticketId && x.RequesterUserId == requesterUserId, cancellationToken)
            .ConfigureAwait(false);
        if (t is null)
            return null;

        var messages = await db.SupportTicketMessages.AsNoTracking()
            .Where(m => m.TicketId == ticketId && !m.IsInternal)
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
            .Where(h => h.TicketId == ticketId && !(h.EventType == "Reply" && h.ToValue == "internal"))
            .OrderBy(h => h.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return MapTorcedorDetail(t, messages, attachmentsByMessage, history);
    }

    public async Task<SupportTicketCreateResult> CreateMyTicketAsync(
        Guid requesterUserId,
        string queue,
        string subject,
        SupportTicketPriority priority,
        string? initialMessage,
        IReadOnlyList<SupportTorcedorAttachmentInput> attachments,
        CancellationToken cancellationToken = default)
    {
        if (!await UserExistsAsync(requesterUserId, cancellationToken).ConfigureAwait(false))
            return new SupportTicketCreateResult(null, SupportTicketMutationError.Validation);

        var attErr = ValidateAttachmentInputs(attachments);
        if (attErr is not null)
            return new SupportTicketCreateResult(null, attErr.Value);

        var hasText = !string.IsNullOrWhiteSpace(initialMessage);
        var hasAtt = attachments.Count > 0;
        if (!hasText && !hasAtt)
            return new SupportTicketCreateResult(null, SupportTicketMutationError.Validation);

        if (ValidateCreateFields(queue, subject) is { } fieldErr)
            return new SupportTicketCreateResult(null, fieldErr);

        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();
        var sla = SupportTicketStateMachine.ComputeSlaDeadline(priority, now);
        var bodyText = hasText ? initialMessage!.Trim() : AttachmentOnlyBodyPlaceholder;

        var row = new SupportTicketRecord
        {
            Id = id,
            RequesterUserId = requesterUserId,
            AssignedAgentUserId = null,
            Queue = queue.Trim(),
            Subject = subject.Trim(),
            Priority = priority,
            Status = SupportTicketStatus.Open,
            SlaDeadlineUtc = sla,
            FirstResponseAtUtc = null,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        IDbContextTransaction? tx = null;
        if (db.Database.IsRelational())
            tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            db.SupportTickets.Add(row);
            db.SupportTicketHistories.Add(
                new SupportTicketHistoryRecord
                {
                    Id = Guid.NewGuid(),
                    TicketId = id,
                    EventType = "Created",
                    FromValue = null,
                    ToValue = SupportTicketStatus.Open.ToString(),
                    ActorUserId = requesterUserId,
                    Reason = null,
                    CreatedAtUtc = now,
                });

            var msg = new SupportTicketMessageRecord
            {
                Id = Guid.NewGuid(),
                TicketId = id,
                AuthorUserId = requesterUserId,
                Body = bodyText,
                IsInternal = false,
                CreatedAtUtc = now,
            };
            db.SupportTicketMessages.Add(msg);
            db.SupportTicketHistories.Add(
                new SupportTicketHistoryRecord
                {
                    Id = Guid.NewGuid(),
                    TicketId = id,
                    EventType = "Reply",
                    FromValue = null,
                    ToValue = "initial",
                    ActorUserId = requesterUserId,
                    Reason = null,
                    CreatedAtUtc = now,
                });
            row.FirstResponseAtUtc = now;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            if (hasAtt)
            {
                foreach (var a in attachments)
                {
                    var key = await attachmentStorage
                        .SaveAsync(id, msg.Id, a.Content, a.FileName, a.ContentType, cancellationToken)
                        .ConfigureAwait(false);
                    if (key is null)
                    {
                        if (tx is not null)
                            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
                        else
                        {
                            db.SupportTickets.Remove(row);
                            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                        }

                        return new SupportTicketCreateResult(null, SupportTicketMutationError.Validation);
                    }

                    db.SupportTicketMessageAttachments.Add(
                        new SupportTicketMessageAttachmentRecord
                        {
                            Id = Guid.NewGuid(),
                            MessageId = msg.Id,
                            OriginalFileName = SanitizeFileName(a.FileName),
                            ContentType = a.ContentType.Trim(),
                            StorageKey = key,
                            SizeBytes = a.Content.Length,
                            CreatedAtUtc = DateTimeOffset.UtcNow,
                        });
                }

                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            if (tx is not null)
                await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            if (tx is not null)
                await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
        finally
        {
            if (tx is not null)
                await tx.DisposeAsync().ConfigureAwait(false);
        }

        return new SupportTicketCreateResult(id, null);
    }

    public async Task<SupportTicketMutationResult> ReplyMyTicketAsync(
        Guid ticketId,
        Guid requesterUserId,
        string body,
        IReadOnlyList<SupportTorcedorAttachmentInput> attachments,
        CancellationToken cancellationToken = default)
    {
        var attErr = ValidateAttachmentInputs(attachments);
        if (attErr is not null)
            return new SupportTicketMutationResult(false, attErr.Value);

        var hasText = !string.IsNullOrWhiteSpace(body);
        var hasAtt = attachments.Count > 0;
        if (!hasText && !hasAtt)
            return new SupportTicketMutationResult(false, SupportTicketMutationError.Validation);

        var bodyText = hasText ? body.Trim() : AttachmentOnlyBodyPlaceholder;

        var ticket = await db.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken).ConfigureAwait(false);
        if (ticket is null || ticket.RequesterUserId != requesterUserId)
            return new SupportTicketMutationResult(false, SupportTicketMutationError.NotFound);

        if (ticket.Status == SupportTicketStatus.Closed)
            return new SupportTicketMutationResult(false, SupportTicketMutationError.Conflict);

        if (!await UserExistsAsync(requesterUserId, cancellationToken).ConfigureAwait(false))
            return new SupportTicketMutationResult(false, SupportTicketMutationError.Validation);

        var now = DateTimeOffset.UtcNow;
        var msg = new SupportTicketMessageRecord
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AuthorUserId = requesterUserId,
            Body = bodyText,
            IsInternal = false,
            CreatedAtUtc = now,
        };

        IDbContextTransaction? tx = null;
        if (db.Database.IsRelational())
            tx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            db.SupportTicketMessages.Add(msg);
            db.SupportTicketHistories.Add(
                new SupportTicketHistoryRecord
                {
                    Id = Guid.NewGuid(),
                    TicketId = ticketId,
                    EventType = "Reply",
                    FromValue = null,
                    ToValue = "public",
                    ActorUserId = requesterUserId,
                    Reason = null,
                    CreatedAtUtc = now,
                });

            ticket.FirstResponseAtUtc ??= now;
            ticket.UpdatedAtUtc = now;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            if (hasAtt)
            {
                foreach (var a in attachments)
                {
                    var key = await attachmentStorage
                        .SaveAsync(ticketId, msg.Id, a.Content, a.FileName, a.ContentType, cancellationToken)
                        .ConfigureAwait(false);
                    if (key is null)
                    {
                        if (tx is not null)
                            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
                        else
                        {
                            db.SupportTicketMessages.Remove(msg);
                            ticket.UpdatedAtUtc = DateTimeOffset.UtcNow;
                            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                        }

                        return new SupportTicketMutationResult(false, SupportTicketMutationError.Validation);
                    }

                    db.SupportTicketMessageAttachments.Add(
                        new SupportTicketMessageAttachmentRecord
                        {
                            Id = Guid.NewGuid(),
                            MessageId = msg.Id,
                            OriginalFileName = SanitizeFileName(a.FileName),
                            ContentType = a.ContentType.Trim(),
                            StorageKey = key,
                            SizeBytes = a.Content.Length,
                            CreatedAtUtc = DateTimeOffset.UtcNow,
                        });
                }

                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            if (tx is not null)
                await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            if (tx is not null)
                await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
        finally
        {
            if (tx is not null)
                await tx.DisposeAsync().ConfigureAwait(false);
        }

        return new SupportTicketMutationResult(true, null);
    }

    public async Task<SupportTicketMutationResult> CancelMyTicketAsync(
        Guid ticketId,
        Guid requesterUserId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var ticket = await db.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken).ConfigureAwait(false);
        if (ticket is null || ticket.RequesterUserId != requesterUserId)
            return new SupportTicketMutationResult(false, SupportTicketMutationError.NotFound);

        if (ticket.Status == SupportTicketStatus.Closed)
            return new SupportTicketMutationResult(false, SupportTicketMutationError.Conflict);

        if (!SupportTicketStateMachine.IsValidTransition(ticket.Status, SupportTicketStatus.Closed))
            return new SupportTicketMutationResult(false, SupportTicketMutationError.InvalidStatusTransition);

        if (!await UserExistsAsync(requesterUserId, cancellationToken).ConfigureAwait(false))
            return new SupportTicketMutationResult(false, SupportTicketMutationError.Validation);

        var now = DateTimeOffset.UtcNow;
        var old = ticket.Status;
        ticket.Status = SupportTicketStatus.Closed;
        ticket.UpdatedAtUtc = now;

        db.SupportTicketHistories.Add(
            new SupportTicketHistoryRecord
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                EventType = "StatusChanged",
                FromValue = old.ToString(),
                ToValue = SupportTicketStatus.Closed.ToString(),
                ActorUserId = requesterUserId,
                Reason = string.IsNullOrWhiteSpace(reason) ? "Cancelado pelo solicitante" : reason.Trim(),
                CreatedAtUtc = now,
            });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new SupportTicketMutationResult(true, null);
    }

    public async Task<SupportTicketMutationResult> ReopenMyTicketAsync(
        Guid ticketId,
        Guid requesterUserId,
        CancellationToken cancellationToken = default)
    {
        var ticket = await db.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken).ConfigureAwait(false);
        if (ticket is null || ticket.RequesterUserId != requesterUserId)
            return new SupportTicketMutationResult(false, SupportTicketMutationError.NotFound);

        if (ticket.Status != SupportTicketStatus.Closed)
            return new SupportTicketMutationResult(false, SupportTicketMutationError.InvalidStatusTransition);

        if (!SupportTicketStateMachine.IsValidTransition(SupportTicketStatus.Closed, SupportTicketStatus.Open))
            return new SupportTicketMutationResult(false, SupportTicketMutationError.InvalidStatusTransition);

        if (!await UserExistsAsync(requesterUserId, cancellationToken).ConfigureAwait(false))
            return new SupportTicketMutationResult(false, SupportTicketMutationError.Validation);

        var now = DateTimeOffset.UtcNow;
        var old = ticket.Status;
        ticket.Status = SupportTicketStatus.Open;
        ticket.UpdatedAtUtc = now;

        db.SupportTicketHistories.Add(
            new SupportTicketHistoryRecord
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                EventType = "StatusChanged",
                FromValue = old.ToString(),
                ToValue = SupportTicketStatus.Open.ToString(),
                ActorUserId = requesterUserId,
                Reason = "Reaberto pelo solicitante",
                CreatedAtUtc = now,
            });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new SupportTicketMutationResult(true, null);
    }

    private SupportTicketMutationError? ValidateAttachmentInputs(IReadOnlyList<SupportTorcedorAttachmentInput> attachments)
    {
        var maxFiles = Math.Max(1, attachmentOptions.Value.MaxFilesPerMessage);
        if (attachments.Count > maxFiles)
            return SupportTicketMutationError.Validation;

        var maxBytes = Math.Max(1, attachmentOptions.Value.MaxBytesPerFile);
        foreach (var a in attachments)
        {
            if (a.Content is null || a.Content.Length == 0)
                return SupportTicketMutationError.Validation;
            if (a.Content.Length > maxBytes)
                return SupportTicketMutationError.Validation;
        }

        return null;
    }

    private static SupportTicketMutationError? ValidateCreateFields(string queue, string subject)
    {
        if (string.IsNullOrWhiteSpace(queue) || queue.Trim().Length > 64)
            return SupportTicketMutationError.Validation;
        if (string.IsNullOrWhiteSpace(subject) || subject.Trim().Length > 500)
            return SupportTicketMutationError.Validation;
        return null;
    }

    private static TorcedorSupportTicketListItemDto MapListItem(SupportTicketRecord t)
    {
        var now = DateTimeOffset.UtcNow;
        var breached = now > t.SlaDeadlineUtc
            && t.Status != SupportTicketStatus.Resolved
            && t.Status != SupportTicketStatus.Closed;
        return new TorcedorSupportTicketListItemDto(
            t.Id,
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

    private static TorcedorSupportTicketDetailDto MapTorcedorDetail(
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
                        att => new TorcedorSupportMessageAttachmentDto(
                            att.Id,
                            att.OriginalFileName,
                            att.ContentType,
                            $"/api/support/tickets/{t.Id}/attachments/{att.Id}"))
                    .ToList();
                return new TorcedorSupportMessageDto(m.Id, m.AuthorUserId, m.Body, m.CreatedAtUtc, atts);
            })
            .ToList();

        var histDtos = history
            .Select(
                h => new TorcedorSupportHistoryEntryDto(
                    h.Id,
                    h.EventType,
                    h.FromValue,
                    h.ToValue,
                    h.ActorUserId,
                    h.Reason,
                    h.CreatedAtUtc))
            .ToList();

        return new TorcedorSupportTicketDetailDto(
            t.Id,
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

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "file";
        var name = Path.GetFileName(fileName.Trim());
        return string.IsNullOrEmpty(name) ? "file" : name[..Math.Min(name.Length, 255)];
    }
}
