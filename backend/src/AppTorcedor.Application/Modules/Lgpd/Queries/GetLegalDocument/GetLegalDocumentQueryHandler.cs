using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Lgpd;
using MediatR;

namespace AppTorcedor.Application.Modules.Lgpd.Queries.GetLegalDocument;

public sealed class GetLegalDocumentQueryHandler(ILgpdAdministrationPort lgpd)
    : IRequestHandler<GetLegalDocumentQuery, LegalDocumentDetailDto?>
{
    public Task<LegalDocumentDetailDto?> Handle(GetLegalDocumentQuery request, CancellationToken cancellationToken) =>
        lgpd.GetDocumentAsync(request.Id, cancellationToken);
}
