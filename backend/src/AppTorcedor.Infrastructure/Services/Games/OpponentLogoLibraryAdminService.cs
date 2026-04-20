using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Games;

public sealed class OpponentLogoLibraryAdminService(AppDbContext db, IOpponentLogoStorage storage) : IOpponentLogoLibraryAdminPort
{
    public async Task<UploadOpponentLogoResult?> UploadAndRegisterAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var url = await storage
            .SaveOpponentLogoAsync(content, fileName, contentType, cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var trimmed = url.Trim();
        var now = DateTimeOffset.UtcNow;
        var row = new OpponentLogoAssetRecord
        {
            Id = Guid.NewGuid(),
            PublicUrl = trimmed,
            CreatedAt = now,
        };
        db.OpponentLogoAssets.Add(row);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new UploadOpponentLogoResult(trimmed);
    }

    public async Task<OpponentLogoAssetListPageDto> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.OpponentLogoAssets.AsNoTracking();
        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = rows
            .Select(x => new OpponentLogoAssetListItemDto(x.Id, x.PublicUrl, x.CreatedAt))
            .ToList();

        return new OpponentLogoAssetListPageDto(total, items);
    }
}
