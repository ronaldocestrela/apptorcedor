using AppTorcedor.Application.Modules.Lgpd;

namespace AppTorcedor.Application.Abstractions;

public interface ILgpdAdministrationPort
{
    Task<IReadOnlyList<LegalDocumentListItemDto>> ListDocumentsAsync(CancellationToken cancellationToken = default);

    Task<LegalDocumentDetailDto?> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default);

    Task<LegalDocumentDetailDto> CreateDocumentAsync(LegalDocumentType type, string title, CancellationToken cancellationToken = default);

    Task<LegalDocumentVersionDetailDto> AddVersionAsync(Guid documentId, string content, CancellationToken cancellationToken = default);

    Task PublishVersionAsync(Guid versionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserConsentRowDto>> ListConsentsForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task RecordConsentAsync(Guid userId, Guid documentVersionId, string? clientIp, CancellationToken cancellationToken = default);

    Task<PrivacyOperationResultDto> ExportUserDataAsync(Guid subjectUserId, Guid requestedByUserId, CancellationToken cancellationToken = default);

    Task<PrivacyOperationResultDto> AnonymizeUserAsync(Guid subjectUserId, Guid requestedByUserId, CancellationToken cancellationToken = default);
}
