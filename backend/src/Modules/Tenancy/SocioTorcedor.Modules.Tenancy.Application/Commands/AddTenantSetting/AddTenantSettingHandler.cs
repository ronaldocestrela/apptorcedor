using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;

namespace SocioTorcedor.Modules.Tenancy.Application.Commands.AddTenantSetting;

public sealed class AddTenantSettingHandler(ITenantRepository repository)
    : ICommandHandler<AddTenantSettingCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddTenantSettingCommand command, CancellationToken cancellationToken)
    {
        var tenant = await repository.GetEntityByIdAsync(command.TenantId, cancellationToken);
        if (tenant is null)
            return Result<Guid>.Fail(Error.NotFound("Tenant.NotFound", "Tenant not found."));

        try
        {
            var existingIds = tenant.Settings.Select(s => s.Id).ToHashSet();
            tenant.AddSetting(command.Key, command.Value);
            await repository.SaveChangesAsync(cancellationToken);
            var added = tenant.Settings.First(s => !existingIds.Contains(s.Id));
            return Result<Guid>.Ok(added.Id);
        }
        catch (InvalidOperationException ex)
        {
            return Result<Guid>.Fail(Error.Conflict("Tenant.SettingExists", ex.Message));
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Fail(Error.Validation("Tenant.Invalid", ex.Message));
        }
    }
}
