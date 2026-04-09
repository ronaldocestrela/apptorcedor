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

    public void ChangeValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.", nameof(value));

        Value = value.Trim();
    }
}
