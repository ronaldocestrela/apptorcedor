using AppTorcedor.Application.Modules.Lgpd;
using MediatR;

namespace AppTorcedor.Application.Modules.Lgpd.Queries.GetLegalDocument;

public sealed record GetLegalDocumentQuery(Guid Id) : IRequest<LegalDocumentDetailDto?>;
