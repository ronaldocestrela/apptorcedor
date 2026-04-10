using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Identity.Application.Contracts;
using SocioTorcedor.Modules.Identity.Application.DTOs;

namespace SocioTorcedor.Modules.Identity.Application.Queries.GetCurrentLegalDocuments;

public sealed class GetCurrentLegalDocumentsHandler(
    ILegalDocumentRepository legalDocuments,
    ICurrentTenantContext tenantContext) : IQueryHandler<GetCurrentLegalDocumentsQuery, CurrentLegalDocumentsDto>
{
    public async Task<Result<CurrentLegalDocumentsDto>> Handle(
        GetCurrentLegalDocumentsQuery query,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.IsResolved)
            return Result<CurrentLegalDocumentsDto>.Fail(
                Error.Failure("Tenant.Required", "Tenant context is not resolved."));

        var current = await legalDocuments.GetCurrentDocumentsAsync(cancellationToken);
        if (current is null)
            return Result<CurrentLegalDocumentsDto>.Fail(
                Error.Failure(
                    "Identity.LegalDocumentsNotConfigured",
                    "Legal documents are not published for this tenant."));

        return Result<CurrentLegalDocumentsDto>.Ok(current);
    }
}
