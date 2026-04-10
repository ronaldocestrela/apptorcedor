using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Identity.Application.DTOs;

namespace SocioTorcedor.Modules.Identity.Application.Contracts;

public interface IIdentityService
{
    Task<Result<AuthResultDto>> RegisterAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        Guid tenantId,
        Guid acceptedTermsDocumentId,
        Guid acceptedPrivacyDocumentId,
        string? consentIpAddress,
        string? consentUserAgent,
        CancellationToken cancellationToken);

    Task<Result<AuthResultDto>> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken);
}
