using AppTorcedor.Application.Modules.Lgpd;
using MediatR;

namespace AppTorcedor.Application.Modules.Lgpd.Queries.ListLegalDocuments;

public sealed record ListLegalDocumentsQuery : IRequest<IReadOnlyList<LegalDocumentListItemDto>>;
