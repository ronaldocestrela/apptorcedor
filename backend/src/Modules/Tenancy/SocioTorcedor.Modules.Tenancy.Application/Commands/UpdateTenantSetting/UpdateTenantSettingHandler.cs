using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;

namespace SocioTorcedor.Modules.Tenancy.Application.Commands.UpdateTenantSetting;

public sealed class UpdateTenantSettingHandler(ITenantRepository repository)
    : ICommandHandler<UpdateTenantSettingCommand>
{
    public async Task<Result> Handle(UpdateTenantSettingCommand command, CancellationToken cancellationToken)
    {
        var tenant = await repository.GetEntityByIdAsync(command.TenantId, cancellationToken);
        if (tenant is null)
            return Result.Fail(Error.NotFound("Tenant.NotFound", "Tenant not found."));

        try
        {
            if (!tenant.UpdateSetting(command.SettingId, command.Value))
                return Result.Fail(Error.NotFound("Tenant.SettingNotFound", "Setting not found for this tenant."));

            await repository.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
        catch (ArgumentException ex)
        {
            return Result.Fail(Error.Validation("Tenant.Invalid", ex.Message));
        }
    }
}
