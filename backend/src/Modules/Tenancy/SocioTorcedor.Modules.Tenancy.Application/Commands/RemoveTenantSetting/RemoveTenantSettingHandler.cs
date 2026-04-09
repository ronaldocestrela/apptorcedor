using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;

namespace SocioTorcedor.Modules.Tenancy.Application.Commands.RemoveTenantSetting;

public sealed class RemoveTenantSettingHandler(ITenantRepository repository)
    : ICommandHandler<RemoveTenantSettingCommand>
{
    public async Task<Result> Handle(RemoveTenantSettingCommand command, CancellationToken cancellationToken)
    {
        var tenant = await repository.GetEntityByIdAsync(command.TenantId, cancellationToken);
        if (tenant is null)
            return Result.Fail(Error.NotFound("Tenant.NotFound", "Tenant not found."));

        if (!tenant.RemoveSetting(command.SettingId))
            return Result.Fail(Error.NotFound("Tenant.SettingNotFound", "Setting not found for this tenant."));

        await repository.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
