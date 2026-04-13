using AppTorcedor.Application.Modules.Lgpd;
using MediatR;

namespace AppTorcedor.Application.Modules.Lgpd.Commands.CreateLegalDocument;

public sealed record CreateLegalDocumentCommand(LegalDocumentType Type, string Title) : IRequest<LegalDocumentDetailDto>;
