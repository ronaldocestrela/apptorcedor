namespace AppTorcedor.Infrastructure.Entities;

public sealed class LegalDocumentRecord
{
    public Guid Id { get; set; }

    public LegalDocumentType Type { get; set; }

    /// <summary>Short title for backoffice (e.g. "Termos de uso").</summary>
    public string Title { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
