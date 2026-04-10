namespace SocioTorcedor.Modules.Identity.Application.DTOs;

public sealed record CurrentLegalDocumentsDto(
    LegalDocumentVersionDto TermsOfUse,
    LegalDocumentVersionDto PrivacyPolicy);
