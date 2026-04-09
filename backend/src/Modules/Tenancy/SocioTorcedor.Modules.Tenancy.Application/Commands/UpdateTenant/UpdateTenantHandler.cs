using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;

namespace SocioTorcedor.Modules.Tenancy.Application.Commands.UpdateTenant;

public sealed class UpdateTenantHandler(
    ITenantRepository repository,
    ITenantSlugCacheInvalidator cacheInvalidator)
    : ICommandHandler<UpdateTenantCommand>
{
    public async Task<Result> Handle(UpdateTenantCommand command, CancellationToken cancellationToken)
    {
        var tenant = await repository.GetEntityByIdAsync(command.TenantId, cancellationToken);
        if (tenant is null)
            return Result.Fail(Error.NotFound("Tenant.NotFound", "Tenant not found."));

        var slug = tenant.Slug;

        try
        {
            if (!string.IsNullOrWhiteSpace(command.Name))
                tenant.UpdateName(command.Name);

            if (!string.IsNullOrWhiteSpace(command.ConnectionString))
                tenant.UpdateConnectionString(command.ConnectionString);
        }
        catch (ArgumentException ex)
        {
            return Result.Fail(Error.Validation("Tenant.Invalid", ex.Message));
        }

        await repository.SaveChangesAsync(cancellationToken);
        cacheInvalidator.Invalidate(slug);
        return Result.Ok();
    }
}
