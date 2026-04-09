using FluentAssertions;
using SocioTorcedor.BuildingBlocks.Domain.Abstractions;
using SocioTorcedor.Modules.Tenancy.Domain.Entities;
using SocioTorcedor.Modules.Tenancy.Domain.Enums;
using SocioTorcedor.Modules.Tenancy.Domain.Events;

namespace SocioTorcedor.Modules.Tenancy.Domain.Tests.Entities;

public class TenantTests
{
    [Fact]
    public void Create_when_slug_free_succeeds_and_raises_domain_event()
    {
        var tenant = Tenant.Create("Flamengo", "flamengo", "Server=.", () => false);

        tenant.Name.Should().Be("Flamengo");
        tenant.Slug.Should().Be("flamengo");
        tenant.Status.Should().Be(TenantStatus.Active);
        tenant.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<TenantCreatedDomainEvent>()
            .Which.TenantId.Should().Be(tenant.Id);
    }

    [Fact]
    public void Create_when_slug_taken_throws_business_rule_exception()
    {
        var act = () => Tenant.Create("A", "dup", "cs", () => true);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.BrokenRule.Message.Should().Contain("slug");
    }

    [Fact]
    public void ChangeStatus_updates_status()
    {
        var tenant = Tenant.Create("T", "t1", "cs", () => false);

        tenant.ChangeStatus(TenantStatus.Suspended);

        tenant.Status.Should().Be(TenantStatus.Suspended);
    }

    [Fact]
    public void AddAllowedOrigin_adds_domain_row()
    {
        var tenant = Tenant.Create("T", "t2", "cs", () => false);

        tenant.AddAllowedOrigin("https://t2.app");

        tenant.Domains.Should().ContainSingle().Which.Origin.Should().Be("https://t2.app");
    }

    [Fact]
    public void RemoveDomain_removes_when_present()
    {
        var tenant = Tenant.Create("T", "t3", "cs", () => false);
        tenant.AddAllowedOrigin("https://a.com");
        var id = tenant.Domains.Single().Id;

        tenant.RemoveDomain(id).Should().BeTrue();
        tenant.Domains.Should().BeEmpty();
    }

    [Fact]
    public void AddSetting_and_UpdateSetting_and_RemoveSetting_work()
    {
        var tenant = Tenant.Create("T", "t4", "cs", () => false);
        var setting = tenant.AddSetting("k1", "v1");
        setting.Id.Should().NotBeEmpty();

        tenant.UpdateSetting(setting.Id, "v2").Should().BeTrue();
        tenant.Settings.Single().Value.Should().Be("v2");

        tenant.RemoveSetting(setting.Id).Should().BeTrue();
        tenant.Settings.Should().BeEmpty();
    }

    [Fact]
    public void AddSetting_duplicate_key_throws()
    {
        var tenant = Tenant.Create("T", "t5", "cs", () => false);
        tenant.AddSetting("dup", "1");

        var act = () => tenant.AddSetting("DUP", "2");

        act.Should().Throw<InvalidOperationException>();
    }
}
