using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Lgpd;
using AppTorcedor.Application.Modules.Lgpd.Commands.PublishLegalDocumentVersion;

namespace AppTorcedor.Application.Tests;

public sealed class PublishLegalDocumentVersionCommandHandlerTests
{
    [Fact]
    public async Task Handle_calls_PublishVersionAsync_on_port()
    {
        var port = new RecordingLgpdPort();
        var handler = new PublishLegalDocumentVersionCommandHandler(port);
        var versionId = Guid.NewGuid();
        await handler.Handle(new PublishLegalDocumentVersionCommand(versionId), default);
        Assert.Equal(versionId, port.PublishedVersionId);
    }

    private sealed class RecordingLgpdPort : ILgpdAdministrationPort
    {
        public Guid? PublishedVersionId { get; private set; }

        public Task PublishVersionAsync(Guid versionId, CancellationToken cancellationToken = default)
        {
            PublishedVersionId = versionId;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<LegalDocumentListItemDto>> ListDocumentsAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<LegalDocumentDetailDto?> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<LegalDocumentDetailDto> CreateDocumentAsync(LegalDocumentType type, string title, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<LegalDocumentVersionDetailDto> AddVersionAsync(Guid documentId, string content, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<UserConsentRowDto>> ListConsentsForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task RecordConsentAsync(Guid userId, Guid documentVersionId, string? clientIp, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PrivacyOperationResultDto> ExportUserDataAsync(Guid subjectUserId, Guid requestedByUserId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PrivacyOperationResultDto> AnonymizeUserAsync(Guid subjectUserId, Guid requestedByUserId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
