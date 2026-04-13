using AppTorcedor.Application.Abstractions;

namespace AppTorcedor.Infrastructure.Entities;

public sealed class InAppNotificationRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid NewsArticleId { get; set; }
    public string Title { get; set; } = "";
    public string? PreviewText { get; set; }
    public DateTimeOffset ScheduledAt { get; set; }
    public DateTimeOffset? DispatchedAt { get; set; }
    public InAppNotificationStatus Status { get; set; }
}
