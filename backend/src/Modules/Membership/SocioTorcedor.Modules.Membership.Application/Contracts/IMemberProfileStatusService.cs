namespace SocioTorcedor.Modules.Membership.Application.Contracts;

/// <summary>
/// Atualiza status de sócio usando connection string explícita (ex.: efeitos de webhook na conta do tenant).
/// </summary>
public interface IMemberProfileStatusService
{
    Task TrySetActiveAsync(string tenantConnectionString, Guid memberProfileId, CancellationToken cancellationToken);

    Task TrySetDelinquentAsync(string tenantConnectionString, Guid memberProfileId, CancellationToken cancellationToken);
}
