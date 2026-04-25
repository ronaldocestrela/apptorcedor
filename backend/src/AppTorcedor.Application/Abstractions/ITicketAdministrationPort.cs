namespace AppTorcedor.Application.Abstractions;

public interface ITicketAdministrationPort
{
    Task<AdminTicketListPageDto> ListTicketsAsync(
        Guid? userId,
        Guid? gameId,
        string? status,
        string? requestStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AdminTicketDetailDto?> GetTicketByIdAsync(Guid ticketId, CancellationToken cancellationToken = default);

    Task<TicketReserveResult> ReserveTicketAsync(Guid userId, Guid gameId, CancellationToken cancellationToken = default);

    Task<TicketMutationResult> PurchaseTicketAsync(Guid ticketId, CancellationToken cancellationToken = default);

    Task<TicketMutationResult> SyncTicketAsync(Guid ticketId, CancellationToken cancellationToken = default);

    Task<TicketMutationResult> RedeemTicketAsync(Guid ticketId, CancellationToken cancellationToken = default);

    Task<TicketMutationResult> UpdateTicketRequestStatusAsync(
        Guid ticketId,
        string requestStatus,
        CancellationToken cancellationToken = default);
}

public sealed record AdminTicketListItemDto(
    Guid TicketId,
    Guid UserId,
    string UserEmail,
    string? UserName,
    Guid GameId,
    string Opponent,
    string Competition,
    DateTimeOffset GameDate,
    string Status,
    string? ExternalTicketId,
    string? QrCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RedeemedAt,
    string RequestStatus,
    string? MembershipPlanName);

public sealed record AdminTicketListPageDto(int TotalCount, IReadOnlyList<AdminTicketListItemDto> Items);

public sealed record AdminTicketDetailDto(
    Guid TicketId,
    Guid UserId,
    string UserEmail,
    string? UserName,
    Guid GameId,
    string Opponent,
    string Competition,
    DateTimeOffset GameDate,
    string Status,
    string? ExternalTicketId,
    string? QrCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? RedeemedAt,
    string RequestStatus,
    string? MembershipPlanName);

public enum TicketMutationError
{
    NotFound,
    GameNotFound,
    UserNotFound,
    GameInactive,
    InvalidTransition,
    ExternalIdMissing,
    ProviderError,
    InvalidRequestStatus,
}

public sealed record TicketMutationResult(bool Ok, TicketMutationError? Error)
{
    public static TicketMutationResult Success() => new(true, null);
    public static TicketMutationResult Fail(TicketMutationError error) => new(false, error);
}

public sealed record TicketReserveResult(Guid? TicketId, TicketMutationError? Error)
{
    public bool Ok => TicketId is not null && Error is null;
}
