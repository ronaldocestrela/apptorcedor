using SocioTorcedor.BuildingBlocks.Domain.Abstractions;

namespace SocioTorcedor.Modules.Backoffice.Domain.Entities;

public sealed class SaaSPlanFeature : Entity
{
    private SaaSPlanFeature()
    {
    }

    internal SaaSPlanFeature(Guid saasPlanId, string key, string? description, string? value)
        : base()
    {
        SaaSPlanId = saasPlanId;
        Key = key;
        Description = description;
        Value = value;
    }

    public Guid SaaSPlanId { get; private set; }

    public string Key { get; private set; } = null!;

    public string? Description { get; private set; }

    public string? Value { get; private set; }

    public SaaSPlan SaaSPlan { get; private set; } = null!;

    public void Update(string key, string? description, string? value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key is required.", nameof(key));

        Key = key.Trim();
        Description = description?.Trim();
        Value = value?.Trim();
    }
}
