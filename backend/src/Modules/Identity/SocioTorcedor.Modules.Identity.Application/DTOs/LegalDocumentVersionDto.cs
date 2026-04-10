using SocioTorcedor.Modules.Identity.Domain.Enums;

namespace SocioTorcedor.Modules.Identity.Application.DTOs;

public sealed record LegalDocumentVersionDto(
    Guid Id,
    LegalDocumentKind Kind,
    int VersionNumber,
    string Content,
    DateTime PublishedAtUtc);
