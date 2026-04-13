namespace AppTorcedor.Application.Abstractions;

/// <summary>Binary payload for an attachment uploaded with a support message (torcedor channel).</summary>
public sealed record SupportTorcedorAttachmentInput(byte[] Content, string FileName, string ContentType);

public sealed record TorcedorSupportTicketListItemDto(
    Guid TicketId,
    string Queue,
    string Subject,
    SupportTicketPriority Priority,
    SupportTicketStatus Status,
    DateTimeOffset SlaDeadlineUtc,
    bool IsSlaBreached,
    DateTimeOffset? FirstResponseAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record TorcedorSupportTicketListPageDto(int TotalCount, IReadOnlyList<TorcedorSupportTicketListItemDto> Items);

public sealed record TorcedorSupportMessageAttachmentDto(
    Guid AttachmentId,
    string FileName,
    string ContentType,
    string DownloadPath);

public sealed record TorcedorSupportMessageDto(
    Guid MessageId,
    Guid AuthorUserId,
    string Body,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<TorcedorSupportMessageAttachmentDto> Attachments);

public sealed record TorcedorSupportHistoryEntryDto(
    Guid EntryId,
    string EventType,
    string? FromValue,
    string? ToValue,
    Guid ActorUserId,
    string? Reason,
    DateTimeOffset CreatedAtUtc);

public sealed record TorcedorSupportTicketDetailDto(
    Guid TicketId,
    string Queue,
    string Subject,
    SupportTicketPriority Priority,
    SupportTicketStatus Status,
    DateTimeOffset SlaDeadlineUtc,
    bool IsSlaBreached,
    DateTimeOffset? FirstResponseAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<TorcedorSupportMessageDto> Messages,
    IReadOnlyList<TorcedorSupportHistoryEntryDto> History);

public interface ISupportTorcedorPort
{
    Task<TorcedorSupportTicketListPageDto> ListMyTicketsAsync(
        Guid requesterUserId,
        SupportTicketStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<TorcedorSupportTicketDetailDto?> GetMyTicketAsync(Guid ticketId, Guid requesterUserId, CancellationToken cancellationToken = default);

    Task<SupportTicketCreateResult> CreateMyTicketAsync(
        Guid requesterUserId,
        string queue,
        string subject,
        SupportTicketPriority priority,
        string? initialMessage,
        IReadOnlyList<SupportTorcedorAttachmentInput> attachments,
        CancellationToken cancellationToken = default);

    Task<SupportTicketMutationResult> ReplyMyTicketAsync(
        Guid ticketId,
        Guid requesterUserId,
        string body,
        IReadOnlyList<SupportTorcedorAttachmentInput> attachments,
        CancellationToken cancellationToken = default);

    Task<SupportTicketMutationResult> CancelMyTicketAsync(
        Guid ticketId,
        Guid requesterUserId,
        string? reason,
        CancellationToken cancellationToken = default);

    Task<SupportTicketMutationResult> ReopenMyTicketAsync(
        Guid ticketId,
        Guid requesterUserId,
        CancellationToken cancellationToken = default);
}

/// <summary>Physical file info after DB lookup (no authorization — caller must enforce).</summary>
public sealed record SupportAttachmentDownloadDto(
    Guid AttachmentId,
    Guid TicketId,
    Guid RequesterUserId,
    string FileName,
    string ContentType,
    string StorageKey);
