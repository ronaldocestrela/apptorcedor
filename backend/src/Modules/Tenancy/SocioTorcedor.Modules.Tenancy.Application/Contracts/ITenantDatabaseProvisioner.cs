namespace SocioTorcedor.Modules.Tenancy.Application.Contracts;

public interface ITenantDatabaseProvisioner
{
    Task ProvisionAsync(string connectionString, CancellationToken cancellationToken);
}
