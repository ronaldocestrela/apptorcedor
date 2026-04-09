using SocioTorcedor.Modules.Tenancy.Domain.Enums;

namespace SocioTorcedor.Modules.Tenancy.Application.DTOs;

public sealed record TenantDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string ConnectionString,
    TenantStatus Status,
    DateTime CreatedAt,
    IReadOnlyList<TenantDomainDto> Domains,
    IReadOnlyList<TenantSettingDto> Settings);
