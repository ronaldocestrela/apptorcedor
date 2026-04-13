namespace AppTorcedor.Application.Abstractions;

public enum SupportTicketStatus
{
    Open = 0,
    InProgress = 1,
    WaitingUser = 2,
    Resolved = 3,
    Closed = 4,
}

public enum SupportTicketPriority
{
    Normal = 0,
    High = 1,
    Urgent = 2,
}

public enum SupportTicketMutationError
{
    NotFound,
    Validation,
    InvalidStatusTransition,
    Conflict,
}

public sealed record AdminSupportTicketListItemDto(
    Guid TicketId,
    Guid RequesterUserId,
    Guid? AssignedAgentUserId,
    string Queue,
    string Subject,
    SupportTicketPriority Priority,
    SupportTicketStatus Status,
    DateTimeOffset SlaDeadlineUtc,
    bool IsSlaBreached,
    DateTimeOffset? FirstResponseAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record AdminSupportTicketListPageDto(int TotalCount, IReadOnlyList<AdminSupportTicketListItemDto> Items);

public sealed record SupportTicketMessageDto(
    Guid MessageId,
    Guid AuthorUserId,
    string Body,
    bool IsInternal,
    DateTimeOffset CreatedAtUtc);

public sealed record SupportTicketHistoryEntryDto(
    Guid EntryId,
    string EventType,
    string? FromValue,
    string? ToValue,
    Guid ActorUserId,
    string? Reason,
    DateTimeOffset CreatedAtUtc);

public sealed record AdminSupportTicketDetailDto(
    Guid TicketId,
    Guid RequesterUserId,
    Guid? AssignedAgentUserId,
    string Queue,
    string Subject,
    SupportTicketPriority Priority,
    SupportTicketStatus Status,
    DateTimeOffset SlaDeadlineUtc,
    bool IsSlaBreached,
    DateTimeOffset? FirstResponseAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<SupportTicketMessageDto> Messages,
    IReadOnlyList<SupportTicketHistoryEntryDto> History);

public sealed record AdminSupportTicketCreateDto(
    Guid RequesterUserId,
    string Queue,
    string Subject,
    SupportTicketPriority Priority,
    string? InitialMessage);

public sealed record SupportTicketCreateResult(Guid? TicketId, SupportTicketMutationError? Error);

public sealed record SupportTicketMutationResult(bool Ok, SupportTicketMutationError? Error);

public interface ISupportAdministrationPort
{
    Task<AdminSupportTicketListPageDto> ListTicketsAsync(
        string? queue,
        SupportTicketStatus? status,
        Guid? assignedUserId,
        bool? unassignedOnly,
        bool? slaBreachedOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AdminSupportTicketDetailDto?> GetTicketByIdAsync(Guid ticketId, CancellationToken cancellationToken = default);

    Task<SupportTicketCreateResult> CreateTicketAsync(
        AdminSupportTicketCreateDto dto,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<SupportTicketMutationResult> ReplyTicketAsync(
        Guid ticketId,
        string body,
        bool isInternal,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<SupportTicketMutationResult> AssignTicketAsync(
        Guid ticketId,
        Guid? agentUserId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<SupportTicketMutationResult> ChangeStatusAsync(
        Guid ticketId,
        SupportTicketStatus newStatus,
        string? reason,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}
