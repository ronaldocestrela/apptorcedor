using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Lgpd.Commands.PublishLegalDocumentVersion;

public sealed class PublishLegalDocumentVersionCommandHandler(ILgpdAdministrationPort lgpd)
    : IRequestHandler<PublishLegalDocumentVersionCommand, Unit>
{
    public async Task<Unit> Handle(PublishLegalDocumentVersionCommand request, CancellationToken cancellationToken)
    {
        await lgpd.PublishVersionAsync(request.VersionId, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
