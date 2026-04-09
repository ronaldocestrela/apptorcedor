using FluentAssertions;
using SocioTorcedor.Modules.Tenancy.Domain.Rules;

namespace SocioTorcedor.Modules.Tenancy.Domain.Tests.Rules;

public class TenantSlugMustBeUniqueRuleTests
{
    [Fact]
    public void IsBroken_true_when_slug_exists()
    {
        var rule = new TenantSlugMustBeUniqueRule(() => true);

        rule.IsBroken().Should().BeTrue();
    }

    [Fact]
    public void IsBroken_false_when_slug_free()
    {
        var rule = new TenantSlugMustBeUniqueRule(() => false);

        rule.IsBroken().Should().BeFalse();
    }
}
