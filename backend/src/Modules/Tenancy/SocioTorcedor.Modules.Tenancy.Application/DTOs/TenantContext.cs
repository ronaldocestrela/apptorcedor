namespace SocioTorcedor.Modules.Tenancy.Application.DTOs;

public sealed record TenantContext(
    Guid TenantId,
    string Name,
    string Subdomain,
    string ConnectionString,
    IReadOnlyList<string> AllowedOrigins);
