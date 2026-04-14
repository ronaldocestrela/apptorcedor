using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Lgpd;
using MediatR;

namespace AppTorcedor.Application.Modules.Lgpd.Commands.AddLegalDocumentVersion;

public sealed class AddLegalDocumentVersionCommandHandler(ILgpdAdministrationPort lgpd)
    : IRequestHandler<AddLegalDocumentVersionCommand, LegalDocumentVersionDetailDto>
{
    public Task<LegalDocumentVersionDetailDto> Handle(AddLegalDocumentVersionCommand request, CancellationToken cancellationToken) =>
        lgpd.AddVersionAsync(request.DocumentId, request.Content, cancellationToken);
}
