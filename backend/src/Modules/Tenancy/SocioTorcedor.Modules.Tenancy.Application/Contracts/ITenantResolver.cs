using SocioTorcedor.Modules.Tenancy.Application.DTOs;

namespace SocioTorcedor.Modules.Tenancy.Application.Contracts;

public interface ITenantResolver
{
    Task<TenantContext?> ResolveAsync(string slug, CancellationToken cancellationToken);
}
