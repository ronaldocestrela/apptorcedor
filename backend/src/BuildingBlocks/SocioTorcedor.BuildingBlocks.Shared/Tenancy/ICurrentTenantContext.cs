namespace SocioTorcedor.BuildingBlocks.Shared.Tenancy;

/// <summary>
/// Request-scoped tenant resolution (populated by host middleware after tenant slug resolution via X-Tenant-Id).
/// </summary>
public interface ICurrentTenantContext
{
    bool IsResolved { get; }

    Guid TenantId { get; }

    /// <summary>SQL connection string for the current tenant database.</summary>
    string TenantConnectionString { get; }
}
