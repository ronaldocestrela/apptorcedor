namespace AppTorcedor.Application.Abstractions;

public interface IGameAdministrationPort
{
    Task<AdminGameListPageDto> ListGamesAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AdminGameDetailDto?> GetGameByIdAsync(Guid gameId, CancellationToken cancellationToken = default);

    Task<GameCreateResult> CreateGameAsync(AdminGameWriteDto dto, CancellationToken cancellationToken = default);

    Task<GameMutationResult> UpdateGameAsync(Guid gameId, AdminGameWriteDto dto, CancellationToken cancellationToken = default);

    Task<GameMutationResult> DeactivateGameAsync(Guid gameId, CancellationToken cancellationToken = default);
}

public sealed record AdminGameWriteDto(
    string Opponent,
    string Competition,
    DateTimeOffset GameDate,
    bool IsActive,
    string? OpponentLogoUrl = null);

public sealed record AdminGameListItemDto(
    Guid GameId,
    string Opponent,
    string Competition,
    string? OpponentLogoUrl,
    DateTimeOffset GameDate,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record AdminGameListPageDto(int TotalCount, IReadOnlyList<AdminGameListItemDto> Items);

public sealed record AdminGameDetailDto(
    Guid GameId,
    string Opponent,
    string Competition,
    string? OpponentLogoUrl,
    DateTimeOffset GameDate,
    bool IsActive,
    DateTimeOffset CreatedAt);

public enum GameMutationError
{
    NotFound,
    Validation,
}

public sealed record GameMutationResult(bool Ok, GameMutationError? Error)
{
    public static GameMutationResult Success() => new(true, null);
    public static GameMutationResult Fail(GameMutationError error) => new(false, error);
}

public sealed record GameCreateResult(Guid? GameId, GameMutationError? Error)
{
    public bool Ok => GameId is not null && Error is null;
}
