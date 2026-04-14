namespace AppTorcedor.Infrastructure.Entities;

public sealed class SupportTicketHistoryRecord
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? FromValue { get; set; }
    public string? ToValue { get; set; }
    public Guid ActorUserId { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
