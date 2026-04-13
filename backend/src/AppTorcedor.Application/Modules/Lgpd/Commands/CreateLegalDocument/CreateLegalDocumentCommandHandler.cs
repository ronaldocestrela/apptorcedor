using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Lgpd;
using MediatR;

namespace AppTorcedor.Application.Modules.Lgpd.Commands.CreateLegalDocument;

public sealed class CreateLegalDocumentCommandHandler(ILgpdAdministrationPort lgpd)
    : IRequestHandler<CreateLegalDocumentCommand, LegalDocumentDetailDto>
{
    public Task<LegalDocumentDetailDto> Handle(CreateLegalDocumentCommand request, CancellationToken cancellationToken) =>
        lgpd.CreateDocumentAsync(request.Type, request.Title, cancellationToken);
}
