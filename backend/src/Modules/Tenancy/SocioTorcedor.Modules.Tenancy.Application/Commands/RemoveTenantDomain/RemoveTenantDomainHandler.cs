using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;

namespace SocioTorcedor.Modules.Tenancy.Application.Commands.RemoveTenantDomain;

public sealed class RemoveTenantDomainHandler(
    ITenantRepository repository,
    ITenantSlugCacheInvalidator cacheInvalidator)
    : ICommandHandler<RemoveTenantDomainCommand>
{
    public async Task<Result> Handle(RemoveTenantDomainCommand command, CancellationToken cancellationToken)
    {
        var tenant = await repository.GetEntityByIdAsync(command.TenantId, cancellationToken);
        if (tenant is null)
            return Result.Fail(Error.NotFound("Tenant.NotFound", "Tenant not found."));

        if (!tenant.RemoveDomain(command.DomainId))
            return Result.Fail(Error.NotFound("Tenant.DomainNotFound", "Domain not found for this tenant."));

        await repository.SaveChangesAsync(cancellationToken);
        cacheInvalidator.Invalidate(tenant.Slug);
        return Result.Ok();
    }
}
