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

    public void AddAllowedOrigin(string origin)
    {
        Domains.Add(new TenantDomain(Id, origin));
    }
}
