using SocioTorcedor.BuildingBlocks.Domain.Abstractions;

namespace SocioTorcedor.Modules.Tenancy.Domain.Entities;

public sealed class TenantDomain : Entity
{
    private TenantDomain()
    {
    }

    public TenantDomain(Guid tenantId, string origin)
        : base()
    {
        TenantId = tenantId;
        Origin = origin;
    }

    public Guid TenantId { get; private set; }

    public string Origin { get; private set; } = null!;

    public Tenant Tenant { get; private set; } = null!;
}
