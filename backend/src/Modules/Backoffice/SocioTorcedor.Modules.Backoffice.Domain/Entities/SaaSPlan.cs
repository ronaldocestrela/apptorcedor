using SocioTorcedor.BuildingBlocks.Domain.Abstractions;

namespace SocioTorcedor.Modules.Backoffice.Domain.Entities;

public sealed class SaaSPlan : AggregateRoot
{
    private SaaSPlan()
    {
    }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public decimal MonthlyPrice { get; private set; }

    public decimal? YearlyPrice { get; private set; }

    public int MaxMembers { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public ICollection<SaaSPlanFeature> Features { get; } = new List<SaaSPlanFeature>();

    public static SaaSPlan Create(
        string name,
        string? description,
        decimal monthlyPrice,
        decimal? yearlyPrice,
        int maxMembers,
        IReadOnlyList<(string Key, string? Description, string? Value)>? features)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        if (monthlyPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(monthlyPrice));

        if (yearlyPrice is < 0)
            throw new ArgumentOutOfRangeException(nameof(yearlyPrice));

        if (maxMembers < 0)
            throw new ArgumentOutOfRangeException(nameof(maxMembers));

        var now = DateTime.UtcNow;
        var plan = new SaaSPlan
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            MonthlyPrice = monthlyPrice,
            YearlyPrice = yearlyPrice,
            MaxMembers = maxMembers,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (features is not null)
        {
            foreach (var f in features)
            {
                if (string.IsNullOrWhiteSpace(f.Key))
                    continue;

                plan.Features.Add(new SaaSPlanFeature(plan.Id, f.Key.Trim(), f.Description?.Trim(), f.Value?.Trim()));
            }
        }

        return plan;
    }

    public void Update(
        string name,
        string? description,
        decimal monthlyPrice,
        decimal? yearlyPrice,
        int maxMembers)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        if (monthlyPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(monthlyPrice));

        if (yearlyPrice is < 0)
            throw new ArgumentOutOfRangeException(nameof(yearlyPrice));

        if (maxMembers < 0)
            throw new ArgumentOutOfRangeException(nameof(maxMembers));

        Name = name.Trim();
        Description = description?.Trim();
        MonthlyPrice = monthlyPrice;
        YearlyPrice = yearlyPrice;
        MaxMembers = maxMembers;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReplaceFeatures(IReadOnlyList<(string Key, string? Description, string? Value)>? features)
    {
        Features.Clear();
        if (features is null)
            return;

        foreach (var f in features)
        {
            if (string.IsNullOrWhiteSpace(f.Key))
                continue;

            Features.Add(new SaaSPlanFeature(Id, f.Key.Trim(), f.Description?.Trim(), f.Value?.Trim()));
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ToggleActive()
    {
        IsActive = !IsActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
