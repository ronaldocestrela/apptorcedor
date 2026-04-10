using FluentAssertions;
using SocioTorcedor.Modules.Membership.Domain.ValueObjects;

namespace SocioTorcedor.Modules.Membership.Domain.Tests.ValueObjects;

public sealed class VantagemTests
{
    [Fact]
    public void Create_trims_description()
    {
        var v = Vantagem.Create("  Desconto em loja  ");
        v.Descricao.Should().Be("Desconto em loja");
    }

    [Fact]
    public void Create_throws_when_description_empty()
    {
        var act = () => Vantagem.Create("  ");
        act.Should().Throw<ArgumentException>().WithParameterName("descricao");
    }

    [Fact]
    public void Equals_compares_description_ordinally()
    {
        var a = Vantagem.Create("Ingresso");
        var b = Vantagem.Create("Ingresso");
        a.Equals(b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}
