using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Games;

public sealed class GameAdministrationService(AppDbContext db) : IGameAdministrationPort
{
    public async Task<AdminGameListPageDto> ListGamesAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Games.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(g =>
                g.Opponent.Contains(s) || g.Competition.Contains(s));
        }

        if (isActive is { } ia)
            query = query.Where(g => g.IsActive == ia);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await query
            .OrderByDescending(g => g.GameDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = rows
            .Select(g => new AdminGameListItemDto(g.Id, g.Opponent, g.Competition, g.GameDate, g.IsActive, g.CreatedAt))
            .ToList();

        return new AdminGameListPageDto(total, items);
    }

    public async Task<AdminGameDetailDto?> GetGameByIdAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var g = await db.Games.AsNoTracking().FirstOrDefaultAsync(x => x.Id == gameId, cancellationToken).ConfigureAwait(false);
        if (g is null)
            return null;
        return new AdminGameDetailDto(g.Id, g.Opponent, g.Competition, g.GameDate, g.IsActive, g.CreatedAt);
    }

    public async Task<GameCreateResult> CreateGameAsync(AdminGameWriteDto dto, CancellationToken cancellationToken = default)
    {
        var err = Validate(dto);
        if (err is not null)
            return new GameCreateResult(null, GameMutationError.Validation);

        var now = DateTimeOffset.UtcNow;
        var row = new GameRecord
        {
            Id = Guid.NewGuid(),
            Opponent = dto.Opponent.Trim(),
            Competition = dto.Competition.Trim(),
            GameDate = dto.GameDate,
            IsActive = dto.IsActive,
            CreatedAt = now,
        };
        db.Games.Add(row);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new GameCreateResult(row.Id, null);
    }

    public async Task<GameMutationResult> UpdateGameAsync(
        Guid gameId,
        AdminGameWriteDto dto,
        CancellationToken cancellationToken = default)
    {
        var err = Validate(dto);
        if (err is not null)
            return GameMutationResult.Fail(GameMutationError.Validation);

        var row = await db.Games.FirstOrDefaultAsync(x => x.Id == gameId, cancellationToken).ConfigureAwait(false);
        if (row is null)
            return GameMutationResult.Fail(GameMutationError.NotFound);

        row.Opponent = dto.Opponent.Trim();
        row.Competition = dto.Competition.Trim();
        row.GameDate = dto.GameDate;
        row.IsActive = dto.IsActive;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return GameMutationResult.Success();
    }

    public async Task<GameMutationResult> DeactivateGameAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var row = await db.Games.FirstOrDefaultAsync(x => x.Id == gameId, cancellationToken).ConfigureAwait(false);
        if (row is null)
            return GameMutationResult.Fail(GameMutationError.NotFound);

        row.IsActive = false;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return GameMutationResult.Success();
    }

    private static string? Validate(AdminGameWriteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Opponent) || dto.Opponent.Trim().Length > 256)
            return "Opponent is required (max 256).";
        if (string.IsNullOrWhiteSpace(dto.Competition) || dto.Competition.Trim().Length > 256)
            return "Competition is required (max 256).";
        return null;
    }
}
