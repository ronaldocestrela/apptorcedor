using SocioTorcedor.BuildingBlocks.Domain.Abstractions;

namespace SocioTorcedor.Modules.Tenancy.Domain.Entities;

public sealed class TenantSetting : Entity
{
    private TenantSetting()
    {
    }

    public TenantSetting(Guid tenantId, string key, string value)
        : base()
    {
        TenantId = tenantId;
        Key = key;
        Value = value;
    }

    public Guid TenantId { get; private set; }

    public string Key { get; private set; } = null!;

    public string Value { get; private set; } = null!;

    public Tenant Tenant { get; private set; } = null!;
}
