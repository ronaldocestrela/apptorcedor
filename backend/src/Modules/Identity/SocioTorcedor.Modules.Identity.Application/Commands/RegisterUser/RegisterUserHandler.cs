using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Identity.Application.Contracts;
using SocioTorcedor.Modules.Identity.Application.DTOs;

namespace SocioTorcedor.Modules.Identity.Application.Commands.RegisterUser;

public sealed class RegisterUserHandler(
    IIdentityService identityService,
    ILegalDocumentRepository legalDocuments,
    ICurrentTenantContext currentTenant)
    : ICommandHandler<RegisterUserCommand, AuthResultDto>
{
    public async Task<Result<AuthResultDto>> Handle(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        if (!currentTenant.IsResolved)
            return Result<AuthResultDto>.Fail(Error.Failure("Tenant.Required", "Tenant context is not resolved."));

        var legal = await legalDocuments.ValidateRegistrationAcceptancesAsync(
            command.AcceptedTermsDocumentId,
            command.AcceptedPrivacyDocumentId,
            cancellationToken);
        if (!legal.IsSuccess)
            return Result<AuthResultDto>.Fail(legal.Error!);

        return await identityService.RegisterAsync(
            command.Email,
            command.Password,
            command.FirstName,
            command.LastName,
            currentTenant.TenantId,
            command.AcceptedTermsDocumentId,
            command.AcceptedPrivacyDocumentId,
            command.ConsentIpAddress,
            command.ConsentUserAgent,
            cancellationToken);
    }
}
