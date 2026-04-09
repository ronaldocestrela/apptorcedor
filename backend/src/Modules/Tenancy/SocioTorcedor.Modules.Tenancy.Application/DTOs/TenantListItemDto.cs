using SocioTorcedor.Modules.Tenancy.Domain.Enums;

namespace SocioTorcedor.Modules.Tenancy.Application.DTOs;

public sealed record TenantListItemDto(
    Guid Id,
    string Name,
    string Slug,
    TenantStatus Status,
    DateTime CreatedAt,
    int DomainCount);
