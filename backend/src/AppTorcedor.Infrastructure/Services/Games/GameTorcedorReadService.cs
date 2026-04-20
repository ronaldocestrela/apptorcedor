using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Games;

public sealed class GameTorcedorReadService(AppDbContext db) : IGameTorcedorReadPort
{
    public async Task<TorcedorGameListPageDto> ListActiveGamesAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Games.AsNoTracking().Where(g => g.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(g => g.Opponent.Contains(s) || g.Competition.Contains(s));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await query
            .OrderByDescending(g => g.GameDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = rows
            .Select(g => new TorcedorGameListItemDto(
                g.Id,
                g.Opponent,
                g.Competition,
                g.OpponentLogoUrl,
                g.GameDate,
                g.CreatedAt))
            .ToList();

        return new TorcedorGameListPageDto(total, items);
    }
}
