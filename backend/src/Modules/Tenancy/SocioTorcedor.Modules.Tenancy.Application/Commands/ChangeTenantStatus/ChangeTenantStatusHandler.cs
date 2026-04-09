using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;

namespace SocioTorcedor.Modules.Tenancy.Application.Commands.ChangeTenantStatus;

public sealed class ChangeTenantStatusHandler(
    ITenantRepository repository,
    ITenantSlugCacheInvalidator cacheInvalidator)
    : ICommandHandler<ChangeTenantStatusCommand>
{
    public async Task<Result> Handle(ChangeTenantStatusCommand command, CancellationToken cancellationToken)
    {
        var tenant = await repository.GetEntityByIdAsync(command.TenantId, cancellationToken);
        if (tenant is null)
            return Result.Fail(Error.NotFound("Tenant.NotFound", "Tenant not found."));

        var slug = tenant.Slug;
        tenant.ChangeStatus(command.Status);
        await repository.SaveChangesAsync(cancellationToken);
        cacheInvalidator.Invalidate(slug);
        return Result.Ok();
    }
}
