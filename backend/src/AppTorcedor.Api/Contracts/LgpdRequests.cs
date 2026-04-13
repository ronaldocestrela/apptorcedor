using AppTorcedor.Application.Modules.Lgpd;

namespace AppTorcedor.Api.Contracts;

public sealed record CreateLegalDocumentRequest(LegalDocumentType Type, string Title);

public sealed record AddLegalDocumentVersionRequest(string Content);

public sealed record RecordUserConsentRequest(Guid DocumentVersionId, string? ClientIp);
