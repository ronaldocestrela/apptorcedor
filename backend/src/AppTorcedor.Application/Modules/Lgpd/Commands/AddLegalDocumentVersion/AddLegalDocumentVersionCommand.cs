using AppTorcedor.Application.Modules.Lgpd;
using MediatR;

namespace AppTorcedor.Application.Modules.Lgpd.Commands.AddLegalDocumentVersion;

public sealed record AddLegalDocumentVersionCommand(Guid DocumentId, string Content) : IRequest<LegalDocumentVersionDetailDto>;
