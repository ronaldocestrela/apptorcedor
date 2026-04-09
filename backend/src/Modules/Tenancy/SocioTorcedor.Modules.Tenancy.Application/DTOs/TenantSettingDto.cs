namespace SocioTorcedor.Modules.Tenancy.Application.DTOs;

public sealed record TenantSettingDto(Guid Id, string Key, string Value);
