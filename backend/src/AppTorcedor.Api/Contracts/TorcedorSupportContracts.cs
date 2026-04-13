using System.ComponentModel.DataAnnotations;
using AppTorcedor.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace AppTorcedor.Api.Contracts;

public sealed class CreateTorcedorSupportTicketForm
{
    [Required]
    [MaxLength(64)]
    public string Queue { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;

    public SupportTicketPriority Priority { get; set; } = SupportTicketPriority.Normal;

    [MaxLength(8000)]
    public string? InitialMessage { get; set; }

    public List<IFormFile>? Attachments { get; set; }
}

public sealed class ReplyTorcedorSupportTicketForm
{
    [MaxLength(8000)]
    public string? Body { get; set; }

    public List<IFormFile>? Attachments { get; set; }
}

public sealed record TorcedorSupportAttachmentResponse(
    Guid AttachmentId,
    string FileName,
    string ContentType,
    string DownloadUrl);

public sealed record TorcedorSupportMessageResponse(
    Guid MessageId,
    Guid AuthorUserId,
    string Body,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<TorcedorSupportAttachmentResponse> Attachments);

public sealed record TorcedorSupportHistoryResponse(
    Guid EntryId,
    string EventType,
    string? FromValue,
    string? ToValue,
    Guid ActorUserId,
    string? Reason,
    DateTimeOffset CreatedAtUtc);

public sealed record TorcedorSupportTicketListItemResponse(
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

public sealed record TorcedorSupportTicketListPageResponse(int TotalCount, IReadOnlyList<TorcedorSupportTicketListItemResponse> Items);

public sealed record TorcedorSupportTicketDetailResponse(
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
    IReadOnlyList<TorcedorSupportMessageResponse> Messages,
    IReadOnlyList<TorcedorSupportHistoryResponse> History);
