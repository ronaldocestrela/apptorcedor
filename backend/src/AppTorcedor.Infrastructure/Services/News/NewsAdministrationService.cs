using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.News;

public sealed class NewsAdministrationService(AppDbContext db) : INewsAdministrationPort
{
    public async Task<AdminNewsListPageDto> ListNewsAsync(
        string? search,
        NewsEditorialStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.NewsArticles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(n => n.Title.Contains(s) || (n.Summary != null && n.Summary.Contains(s)));
        }

        if (status is { } st)
            query = query.Where(n => n.Status == st);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await query
            .OrderByDescending(n => n.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = rows
            .Select(n => new AdminNewsListItemDto(
                n.Id,
                n.Title,
                n.Status,
                n.CreatedAt,
                n.UpdatedAt,
                n.PublishedAt,
                n.UnpublishedAt))
            .ToList();

        return new AdminNewsListPageDto(total, items);
    }

    public async Task<AdminNewsDetailDto?> GetNewsByIdAsync(Guid newsId, CancellationToken cancellationToken = default)
    {
        var n = await db.NewsArticles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == newsId, cancellationToken).ConfigureAwait(false);
        if (n is null)
            return null;
        return new AdminNewsDetailDto(
            n.Id,
            n.Title,
            n.Summary,
            n.Content,
            n.Status,
            n.CreatedAt,
            n.UpdatedAt,
            n.PublishedAt,
            n.UnpublishedAt);
    }

    public async Task<NewsCreateResult> CreateNewsAsync(AdminNewsWriteDto dto, CancellationToken cancellationToken = default)
    {
        var err = ValidateWrite(dto);
        if (err is not null)
            return new NewsCreateResult(null, NewsMutationError.Validation);

        var now = DateTimeOffset.UtcNow;
        var row = new NewsArticleRecord
        {
            Id = Guid.NewGuid(),
            Title = dto.Title.Trim(),
            Summary = string.IsNullOrWhiteSpace(dto.Summary) ? null : dto.Summary.Trim(),
            Content = dto.Content.Trim(),
            Status = NewsEditorialStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
            PublishedAt = null,
            UnpublishedAt = null,
        };
        db.NewsArticles.Add(row);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new NewsCreateResult(row.Id, null);
    }

    public async Task<NewsMutationResult> UpdateNewsAsync(Guid newsId, AdminNewsWriteDto dto, CancellationToken cancellationToken = default)
    {
        var err = ValidateWrite(dto);
        if (err is not null)
            return NewsMutationResult.Fail(NewsMutationError.Validation);

        var row = await db.NewsArticles.FirstOrDefaultAsync(x => x.Id == newsId, cancellationToken).ConfigureAwait(false);
        if (row is null)
            return NewsMutationResult.Fail(NewsMutationError.NotFound);

        row.Title = dto.Title.Trim();
        row.Summary = string.IsNullOrWhiteSpace(dto.Summary) ? null : dto.Summary.Trim();
        row.Content = dto.Content.Trim();
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return NewsMutationResult.Success();
    }

    public async Task<NewsMutationResult> PublishNewsAsync(Guid newsId, CancellationToken cancellationToken = default)
    {
        var row = await db.NewsArticles.FirstOrDefaultAsync(x => x.Id == newsId, cancellationToken).ConfigureAwait(false);
        if (row is null)
            return NewsMutationResult.Fail(NewsMutationError.NotFound);

        if (row.Status == NewsEditorialStatus.Published)
            return NewsMutationResult.Success();

        if (row.Status != NewsEditorialStatus.Draft && row.Status != NewsEditorialStatus.Unpublished)
            return NewsMutationResult.Fail(NewsMutationError.InvalidState);

        var now = DateTimeOffset.UtcNow;
        row.Status = NewsEditorialStatus.Published;
        row.PublishedAt = now;
        row.UnpublishedAt = null;
        row.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return NewsMutationResult.Success();
    }

    public async Task<NewsMutationResult> UnpublishNewsAsync(Guid newsId, CancellationToken cancellationToken = default)
    {
        var row = await db.NewsArticles.FirstOrDefaultAsync(x => x.Id == newsId, cancellationToken).ConfigureAwait(false);
        if (row is null)
            return NewsMutationResult.Fail(NewsMutationError.NotFound);

        if (row.Status != NewsEditorialStatus.Published)
            return NewsMutationResult.Fail(NewsMutationError.InvalidState);

        var now = DateTimeOffset.UtcNow;
        row.Status = NewsEditorialStatus.Unpublished;
        row.UnpublishedAt = now;
        row.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return NewsMutationResult.Success();
    }

    public async Task<NewsNotificationCreateResult> CreateInAppNotificationsForNewsAsync(
        Guid newsId,
        DateTimeOffset? scheduledAt,
        IReadOnlyList<Guid>? targetUserIds,
        CancellationToken cancellationToken = default)
    {
        var article = await db.NewsArticles.FirstOrDefaultAsync(x => x.Id == newsId, cancellationToken).ConfigureAwait(false);
        if (article is null)
            return NewsNotificationCreateResult.Fail(NewsNotificationError.NotFound);

        if (article.Status != NewsEditorialStatus.Published)
            return NewsNotificationCreateResult.Fail(NewsNotificationError.InvalidState);

        IReadOnlyList<Guid> userIds;
        if (targetUserIds is { Count: > 0 })
        {
            userIds = targetUserIds.Distinct().ToList();
            var existing = await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).Select(u => u.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
            if (existing.Count != userIds.Count)
                return NewsNotificationCreateResult.Fail(NewsNotificationError.Validation);
        }
        else
        {
            userIds = await db.Users.AsNoTracking()
                .Where(u => u.IsActive)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        if (userIds.Count == 0)
            return NewsNotificationCreateResult.Success(0);

        var now = DateTimeOffset.UtcNow;
        var schedule = scheduledAt ?? now;
        var immediate = schedule <= now;
        var preview = TruncatePreview(article.Summary ?? article.Title, 500);

        foreach (var userId in userIds)
        {
            db.InAppNotifications.Add(
                new InAppNotificationRecord
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    NewsArticleId = article.Id,
                    Title = article.Title,
                    PreviewText = preview,
                    ScheduledAt = schedule,
                    DispatchedAt = immediate ? now : null,
                    Status = immediate ? InAppNotificationStatus.Dispatched : InAppNotificationStatus.Pending,
                });
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return NewsNotificationCreateResult.Success(userIds.Count);
    }

    private static string? ValidateWrite(AdminNewsWriteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Trim().Length > 256)
            return "Title required, max 256.";
        if (dto.Summary is not null && dto.Summary.Length > 2000)
            return "Summary max 2000.";
        if (string.IsNullOrWhiteSpace(dto.Content) || dto.Content.Length > 200_000)
            return "Content required, max 200000.";
        return null;
    }

    private static string? TruncatePreview(string? text, int max)
    {
        if (string.IsNullOrEmpty(text))
            return null;
        return text.Length <= max ? text : text[..max];
    }
}
