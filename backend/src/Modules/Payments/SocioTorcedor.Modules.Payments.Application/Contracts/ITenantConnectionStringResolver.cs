namespace SocioTorcedor.Modules.Payments.Application.Contracts;

public interface ITenantConnectionStringResolver
{
    Task<string?> GetConnectionStringAsync(Guid tenantId, CancellationToken cancellationToken);
}
