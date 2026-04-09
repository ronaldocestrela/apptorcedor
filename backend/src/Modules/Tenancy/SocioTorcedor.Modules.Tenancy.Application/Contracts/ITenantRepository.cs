using SocioTorcedor.Modules.Tenancy.Application.DTOs;

namespace SocioTorcedor.Modules.Tenancy.Application.Contracts;

public interface ITenantRepository
{
    Task<TenantDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
}
