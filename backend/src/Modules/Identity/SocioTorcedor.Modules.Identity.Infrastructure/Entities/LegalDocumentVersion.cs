using SocioTorcedor.Modules.Identity.Domain.Enums;

namespace SocioTorcedor.Modules.Identity.Infrastructure.Entities;

public sealed class LegalDocumentVersion
{
    public Guid Id { get; set; }

    public LegalDocumentKind Kind { get; set; }

    public int VersionNumber { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime PublishedAtUtc { get; set; }

    public bool IsCurrent { get; set; }
}
