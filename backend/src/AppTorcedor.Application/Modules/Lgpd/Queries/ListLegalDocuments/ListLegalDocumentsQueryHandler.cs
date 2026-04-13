using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Lgpd;
using MediatR;

namespace AppTorcedor.Application.Modules.Lgpd.Queries.ListLegalDocuments;

public sealed class ListLegalDocumentsQueryHandler(ILgpdAdministrationPort lgpd)
    : IRequestHandler<ListLegalDocumentsQuery, IReadOnlyList<LegalDocumentListItemDto>>
{
    public Task<IReadOnlyList<LegalDocumentListItemDto>> Handle(ListLegalDocumentsQuery request, CancellationToken cancellationToken) =>
        lgpd.ListDocumentsAsync(cancellationToken);
}
