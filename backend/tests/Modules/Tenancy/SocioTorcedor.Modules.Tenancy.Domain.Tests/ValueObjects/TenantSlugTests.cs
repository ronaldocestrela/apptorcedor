using FluentAssertions;
using TenantSlugVo = SocioTorcedor.Modules.Tenancy.Domain.ValueObjects.TenantSlug;

namespace SocioTorcedor.Modules.Tenancy.Domain.Tests.ValueObjects;

public class TenantSlugTests
{
    [Theory]
    [InlineData("flamengo")]
    [InlineData("feira-fc")]
    [InlineData("ab")]
    public void Create_accepts_valid_values(string raw)
    {
        var s = TenantSlugVo.Create(raw);

        s.Value.Should().Be(raw.Trim().ToLowerInvariant());
    }

    [Fact]
    public void Create_rejects_empty()
    {
        var act = () => TenantSlugVo.Create(" ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_rejects_uppercase_input_normalizes()
    {
        var s = TenantSlugVo.Create("Flamengo");

        s.Value.Should().Be("flamengo");
    }

    [Theory]
    [InlineData("a")] // too short after rules? min 2
    [InlineData("bad_underscore")]
    public void Create_rejects_invalid(string raw)
    {
        var act = () => TenantSlugVo.Create(raw);

        act.Should().Throw<ArgumentException>();
    }
}
