using FluentAssertions;
using SubdomainVo = SocioTorcedor.Modules.Tenancy.Domain.ValueObjects.Subdomain;

namespace SocioTorcedor.Modules.Tenancy.Domain.Tests.ValueObjects;

public class SubdomainTests
{
    [Theory]
    [InlineData("flamengo")]
    [InlineData("feira-fc")]
    [InlineData("ab")]
    public void Create_accepts_valid_values(string raw)
    {
        var s = SubdomainVo.Create(raw);

        s.Value.Should().Be(raw.Trim().ToLowerInvariant());
    }

    [Fact]
    public void Create_rejects_empty()
    {
        var act = () => SubdomainVo.Create(" ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_rejects_uppercase_input_normalizes()
    {
        var s = SubdomainVo.Create("Flamengo");

        s.Value.Should().Be("flamengo");
    }

    [Theory]
    [InlineData("a")] // too short after rules? min 2
    [InlineData("bad_underscore")]
    public void Create_rejects_invalid(string raw)
    {
        var act = () => SubdomainVo.Create(raw);

        act.Should().Throw<ArgumentException>();
    }
}
