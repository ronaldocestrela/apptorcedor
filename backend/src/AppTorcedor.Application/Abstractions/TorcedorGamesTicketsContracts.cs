namespace AppTorcedor.Application.Abstractions;

public sealed record TorcedorGameListItemDto(
    Guid GameId,
    string Opponent,
    string Competition,
    DateTimeOffset GameDate,
    DateTimeOffset CreatedAt);

public sealed record TorcedorGameListPageDto(int TotalCount, IReadOnlyList<TorcedorGameListItemDto> Items);

public sealed record TorcedorTicketListItemDto(
    Guid TicketId,
    Guid GameId,
    string Opponent,
    string Competition,
    DateTimeOffset GameDate,
    string Status,
    string? ExternalTicketId,
    string? QrCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RedeemedAt);

public sealed record TorcedorTicketListPageDto(int TotalCount, IReadOnlyList<TorcedorTicketListItemDto> Items);

public sealed record TorcedorTicketDetailDto(
    Guid TicketId,
    Guid GameId,
    string Opponent,
    string Competition,
    DateTimeOffset GameDate,
    string Status,
    string? ExternalTicketId,
    string? QrCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? RedeemedAt);

/// <summary>Read-only games catalog for the authenticated supporter (active games only).</summary>
public interface IGameTorcedorReadPort
{
    Task<TorcedorGameListPageDto> ListActiveGamesAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

/// <summary>Tickets owned by the authenticated user (list, detail, redeem).</summary>
public interface ITicketTorcedorPort
{
    Task<TorcedorTicketListPageDto> ListMyTicketsAsync(
        Guid userId,
        Guid? gameId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<TorcedorTicketDetailDto?> GetMyTicketAsync(Guid userId, Guid ticketId, CancellationToken cancellationToken = default);

    Task<TicketMutationResult> RedeemMyTicketAsync(Guid userId, Guid ticketId, CancellationToken cancellationToken = default);
}
