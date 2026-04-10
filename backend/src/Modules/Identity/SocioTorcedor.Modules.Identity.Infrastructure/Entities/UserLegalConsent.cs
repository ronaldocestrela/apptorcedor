using SocioTorcedor.Modules.Identity.Domain.Enums;

namespace SocioTorcedor.Modules.Identity.Infrastructure.Entities;

public sealed class UserLegalConsent
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public LegalDocumentKind Kind { get; set; }

    public Guid LegalDocumentVersionId { get; set; }

    public LegalDocumentVersion? LegalDocumentVersion { get; set; }

    public DateTime AcceptedAtUtc { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }
}
