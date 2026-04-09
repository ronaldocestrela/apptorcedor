using SocioTorcedor.BuildingBlocks.Application.Abstractions;

namespace SocioTorcedor.Modules.Tenancy.Application.Commands.RemoveTenantSetting;

public sealed record RemoveTenantSettingCommand(Guid TenantId, Guid SettingId) : ICommand;
