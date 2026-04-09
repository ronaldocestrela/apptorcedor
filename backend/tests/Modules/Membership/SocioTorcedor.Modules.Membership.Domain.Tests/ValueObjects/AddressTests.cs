using FluentAssertions;
using SocioTorcedor.Modules.Membership.Domain.ValueObjects;

namespace SocioTorcedor.Modules.Membership.Domain.Tests.ValueObjects;

public sealed class AddressTests
{
    [Fact]
    public void Create_normalizes_state_and_cep_digits()
    {
        var a = Address.Create(
            "  Rua X  ",
            "10",
            "ap 2",
            "Bairro",
            "São Paulo",
            "sp",
            "01310-100");

        a.Street.Should().Be("Rua X");
        a.State.Should().Be("SP");
        a.ZipCode.Should().Be("01310100");
        a.Complement.Should().Be("ap 2");
    }

    [Fact]
    public void Create_throws_when_uf_invalid_length()
    {
        var act = () => Address.Create("Rua", "1", null, "B", "C", "SPX", "01310100");
        act.Should().Throw<ArgumentException>().Which.Message.Should().Contain("UF");
    }

    [Fact]
    public void Create_throws_when_cep_not_8_digits()
    {
        var act = () => Address.Create("Rua", "1", null, "B", "C", "SP", "123");
        act.Should().Throw<ArgumentException>().Which.Message.Should().Contain("8");
    }
}
