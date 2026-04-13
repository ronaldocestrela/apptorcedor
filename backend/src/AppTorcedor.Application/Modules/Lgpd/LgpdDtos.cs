namespace AppTorcedor.Application.Modules.Lgpd;

public sealed record LegalDocumentListItemDto(
    Guid Id,
    LegalDocumentType Type,
    string Title,
    DateTimeOffset CreatedAt,
    int? PublishedVersionNumber,
    Guid? PublishedVersionId);

public sealed record LegalDocumentVersionDetailDto(
    Guid Id,
    Guid LegalDocumentId,
    int VersionNumber,
    string Content,
    string Status,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt);

public sealed record LegalDocumentDetailDto(
    Guid Id,
    LegalDocumentType Type,
    string Title,
    DateTimeOffset CreatedAt,
    IReadOnlyList<LegalDocumentVersionDetailDto> Versions);

public sealed record UserConsentRowDto(
    Guid Id,
    Guid UserId,
    Guid LegalDocumentVersionId,
    int DocumentVersionNumber,
    LegalDocumentType DocumentType,
    string DocumentTitle,
    DateTimeOffset AcceptedAt,
    string? ClientIp);

public sealed record PrivacyOperationResultDto(
    Guid RequestId,
    string Kind,
    string Status,
    string? ResultJson,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
