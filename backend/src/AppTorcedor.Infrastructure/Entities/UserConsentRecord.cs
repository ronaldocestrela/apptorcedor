namespace AppTorcedor.Infrastructure.Entities;

public sealed class UserConsentRecord
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid LegalDocumentVersionId { get; set; }

    public DateTimeOffset AcceptedAt { get; set; }

    /// <summary>Optional client IP at acceptance (max IPv6).</summary>
    public string? ClientIp { get; set; }
}
