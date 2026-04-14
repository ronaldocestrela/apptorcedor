namespace AppTorcedor.Infrastructure.Entities;

public sealed class SupportTicketMessageRecord
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
