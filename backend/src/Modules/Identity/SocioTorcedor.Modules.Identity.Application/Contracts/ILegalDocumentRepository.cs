using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Identity.Application.DTOs;
using SocioTorcedor.Modules.Identity.Domain.Enums;

namespace SocioTorcedor.Modules.Identity.Application.Contracts;

public interface ILegalDocumentRepository
{
    Task<CurrentLegalDocumentsDto?> GetCurrentDocumentsAsync(CancellationToken cancellationToken);

    Task<Result> ValidateRegistrationAcceptancesAsync(
        Guid termsDocumentId,
        Guid privacyDocumentId,
        CancellationToken cancellationToken);

    Task PublishNewVersionAsync(LegalDocumentKind kind, string content, CancellationToken cancellationToken);

    Task SaveUserConsentsAsync(
        string userId,
        Guid termsDocumentId,
        Guid privacyDocumentId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);
}
