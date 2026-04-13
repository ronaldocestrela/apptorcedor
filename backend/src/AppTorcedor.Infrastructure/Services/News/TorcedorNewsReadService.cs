using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.News;

public sealed class TorcedorNewsReadService(AppDbContext db) : ITorcedorNewsReadPort
{
    public async Task<TorcedorNewsFeedPageDto> ListPublishedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.NewsArticles.AsNoTracking().Where(n => n.Status == NewsEditorialStatus.Published);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(n => n.Title.Contains(s) || (n.Summary != null && n.Summary.Contains(s)));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await query
            .OrderByDescending(n => n.PublishedAt ?? n.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = rows
            .Select(n => new TorcedorNewsFeedItemDto(
                n.Id,
                n.Title,
                n.Summary,
                n.PublishedAt ?? n.UpdatedAt,
                n.UpdatedAt))
            .ToList();

        return new TorcedorNewsFeedPageDto(total, items);
    }

    public async Task<TorcedorNewsDetailDto?> GetPublishedByIdAsync(Guid newsId, CancellationToken cancellationToken = default)
    {
        var n = await db.NewsArticles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == newsId && x.Status == NewsEditorialStatus.Published, cancellationToken)
            .ConfigureAwait(false);
        if (n is null)
            return null;
        return new TorcedorNewsDetailDto(
            n.Id,
            n.Title,
            n.Summary,
            n.Content,
            n.PublishedAt ?? n.UpdatedAt,
            n.UpdatedAt);
    }
}
