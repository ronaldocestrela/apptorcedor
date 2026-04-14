namespace AppTorcedor.Infrastructure.Entities;

public sealed class LegalDocumentVersionRecord
{
    public Guid Id { get; set; }

    public Guid LegalDocumentId { get; set; }

    public int VersionNumber { get; set; }

    public string Content { get; set; } = string.Empty;

    public LegalDocumentVersionStatus Status { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
