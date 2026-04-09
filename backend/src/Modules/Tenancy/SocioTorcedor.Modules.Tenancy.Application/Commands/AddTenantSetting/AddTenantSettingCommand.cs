using SocioTorcedor.BuildingBlocks.Application.Abstractions;

namespace SocioTorcedor.Modules.Tenancy.Application.Commands.AddTenantSetting;

public sealed record AddTenantSettingCommand(Guid TenantId, string Key, string Value) : ICommand<Guid>;
