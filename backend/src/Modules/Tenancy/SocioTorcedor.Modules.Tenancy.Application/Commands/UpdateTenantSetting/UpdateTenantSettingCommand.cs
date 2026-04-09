using SocioTorcedor.BuildingBlocks.Application.Abstractions;

namespace SocioTorcedor.Modules.Tenancy.Application.Commands.UpdateTenantSetting;

public sealed record UpdateTenantSettingCommand(Guid TenantId, Guid SettingId, string Value) : ICommand;
