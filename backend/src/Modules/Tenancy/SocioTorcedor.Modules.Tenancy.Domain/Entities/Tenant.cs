using SocioTorcedor.BuildingBlocks.Domain.Abstractions;
using SocioTorcedor.Modules.Tenancy.Domain.Enums;
using SocioTorcedor.Modules.Tenancy.Domain.Events;
using SocioTorcedor.Modules.Tenancy.Domain.Rules;
using TenantSlugVo = SocioTorcedor.Modules.Tenancy.Domain.ValueObjects.TenantSlug;

namespace SocioTorcedor.Modules.Tenancy.Domain.Entities;

public sealed class Tenant : AggregateRoot
{
    private Tenant()
    {
    }

    public string Name { get; private set; } = null!;

    public string Slug { get; private set; } = null!;

    public string ConnectionString { get; private set; } = null!;

    public TenantStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public ICollection<TenantDomain> Domains { get; } = new List<TenantDomain>();

    public ICollection<TenantSetting> Settings { get; } = new List<TenantSetting>();

    public static Tenant Create(
        string name,
        string slugRaw,
        string connectionString,
        Func<bool> slugAlreadyExists)
    {
        var rule = new TenantSlugMustBeUniqueRule(slugAlreadyExists);
        if (rule.IsBroken())
            throw new BusinessRuleValidationException(rule);

        var slug = TenantSlugVo.Create(slugRaw);
        var tenant = new Tenant
        {
            Name = name,
            Slug = slug.Value,
            ConnectionString = connectionString,
            Status = TenantStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        tenant.AddDomainEvent(new TenantCreatedDomainEvent(tenant.Id));
        return tenant;
    }

    public void ChangeStatus(TenantStatus status) => Status = status;

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Name = name.Trim();
    }

    public void UpdateConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is required.", nameof(connectionString));

        ConnectionString = connectionString.Trim();
    }

    public void AddAllowedOrigin(string origin)
    {
        Domains.Add(new TenantDomain(Id, origin));
    }

    public bool RemoveDomain(Guid domainId)
    {
        var domain = Domains.FirstOrDefault(d => d.Id == domainId);
        if (domain is null)
            return false;

        Domains.Remove(domain);
        return true;
    }

    public TenantSetting AddSetting(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key is required.", nameof(key));

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.", nameof(value));

        var normalizedKey = key.Trim();
        if (Settings.Any(s => string.Equals(s.Key, normalizedKey, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A setting with key '{normalizedKey}' already exists.");

        var setting = new TenantSetting(Id, normalizedKey, value.Trim());
        Settings.Add(setting);
        return setting;
    }

    public bool UpdateSetting(Guid settingId, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.", nameof(value));

        var setting = Settings.FirstOrDefault(s => s.Id == settingId);
        if (setting is null)
            return false;

        setting.ChangeValue(value.Trim());
        return true;
    }

    public bool RemoveSetting(Guid settingId)
    {
        var setting = Settings.FirstOrDefault(s => s.Id == settingId);
        if (setting is null)
            return false;

        Settings.Remove(setting);
        return true;
    }
}
