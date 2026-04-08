using SocioTorcedor.Modules.Tenancy.Application.DTOs;

namespace SocioTorcedor.Modules.Tenancy.Application.Contracts;

public interface ITenantRepository
{
    Task<TenantDto?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken);
}
