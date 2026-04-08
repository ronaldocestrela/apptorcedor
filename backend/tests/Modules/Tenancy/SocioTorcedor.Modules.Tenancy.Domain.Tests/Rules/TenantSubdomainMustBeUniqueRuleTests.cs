using FluentAssertions;
using SocioTorcedor.Modules.Tenancy.Domain.Rules;

namespace SocioTorcedor.Modules.Tenancy.Domain.Tests.Rules;

public class TenantSubdomainMustBeUniqueRuleTests
{
    [Fact]
    public void IsBroken_true_when_subdomain_exists()
    {
        var rule = new TenantSubdomainMustBeUniqueRule(() => true);

        rule.IsBroken().Should().BeTrue();
    }

    [Fact]
    public void IsBroken_false_when_subdomain_free()
    {
        var rule = new TenantSubdomainMustBeUniqueRule(() => false);

        rule.IsBroken().Should().BeFalse();
    }
}
