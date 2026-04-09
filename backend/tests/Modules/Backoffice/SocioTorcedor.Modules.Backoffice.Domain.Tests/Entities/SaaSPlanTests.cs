using FluentAssertions;
using SocioTorcedor.Modules.Backoffice.Domain.Entities;

namespace SocioTorcedor.Modules.Backoffice.Domain.Tests.Entities;

public sealed class SaaSPlanTests
{
    [Fact]
    public void Create_adds_features_with_plan_id()
    {
        var features = new List<(string Key, string? Description, string? Value)>
        {
            ("f1", "d1", "v1")
        };

        var plan = SaaSPlan.Create("Pro", "Desc", 10m, 100m, 500, features);

        plan.Name.Should().Be("Pro");
        plan.Features.Should().ContainSingle();
        plan.Features.Single().SaaSPlanId.Should().Be(plan.Id);
        plan.Features.Single().Key.Should().Be("f1");
    }

    [Fact]
    public void ToggleActive_flips_flag()
    {
        var plan = SaaSPlan.Create("Basic", null, 0m, null, 0, null);
        plan.IsActive.Should().BeTrue();

        plan.ToggleActive();
        plan.IsActive.Should().BeFalse();

        plan.ToggleActive();
        plan.IsActive.Should().BeTrue();
    }
}
