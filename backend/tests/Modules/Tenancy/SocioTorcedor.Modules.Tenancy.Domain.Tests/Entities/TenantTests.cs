using FluentAssertions;
using SocioTorcedor.BuildingBlocks.Domain.Abstractions;
using SocioTorcedor.Modules.Tenancy.Domain.Entities;
using SocioTorcedor.Modules.Tenancy.Domain.Enums;
using SocioTorcedor.Modules.Tenancy.Domain.Events;

namespace SocioTorcedor.Modules.Tenancy.Domain.Tests.Entities;

public class TenantTests
{
    [Fact]
    public void Create_when_subdomain_free_succeeds_and_raises_domain_event()
    {
        var tenant = Tenant.Create("Flamengo", "flamengo", "Server=.", () => false);

        tenant.Name.Should().Be("Flamengo");
        tenant.Subdomain.Should().Be("flamengo");
        tenant.Status.Should().Be(TenantStatus.Active);
        tenant.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<TenantCreatedDomainEvent>()
            .Which.TenantId.Should().Be(tenant.Id);
    }

    [Fact]
    public void Create_when_subdomain_taken_throws_business_rule_exception()
    {
        var act = () => Tenant.Create("A", "dup", "cs", () => true);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.BrokenRule.Message.Should().Contain("Subdomain");
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
}
