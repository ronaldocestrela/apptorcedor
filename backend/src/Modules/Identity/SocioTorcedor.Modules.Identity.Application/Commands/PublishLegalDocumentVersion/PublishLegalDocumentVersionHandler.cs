using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Identity.Application.Contracts;

namespace SocioTorcedor.Modules.Identity.Application.Commands.PublishLegalDocumentVersion;

public sealed class PublishLegalDocumentVersionHandler(
    ILegalDocumentRepository legalDocuments,
    ICurrentTenantContext tenantContext) : ICommandHandler<PublishLegalDocumentVersionCommand>
{
    public async Task<Result> Handle(
        PublishLegalDocumentVersionCommand command,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved)
            return Result.Fail(Error.Failure("Tenant.Required", "Tenant context is not resolved."));

        try
        {
            await legalDocuments.PublishNewVersionAsync(command.Kind, command.Content, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return Result.Fail(Error.Validation("Identity.InvalidLegalDocument", ex.Message));
        }

        return Result.Ok();
    }
}
