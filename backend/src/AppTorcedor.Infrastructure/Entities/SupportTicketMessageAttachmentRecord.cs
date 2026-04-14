namespace AppTorcedor.Infrastructure.Entities;

public sealed class SupportTicketMessageAttachmentRecord
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    /// <summary>Relative key under configured storage root (not publicly served).</summary>
    public string StorageKey { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
