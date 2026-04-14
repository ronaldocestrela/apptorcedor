using MediatR;

namespace AppTorcedor.Application.Modules.Lgpd.Commands.PublishLegalDocumentVersion;

public sealed record PublishLegalDocumentVersionCommand(Guid VersionId) : IRequest<Unit>;
