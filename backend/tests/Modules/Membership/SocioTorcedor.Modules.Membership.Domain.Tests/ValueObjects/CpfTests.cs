using FluentAssertions;
using SocioTorcedor.Modules.Membership.Domain.ValueObjects;

namespace SocioTorcedor.Modules.Membership.Domain.Tests.ValueObjects;

public sealed class CpfTests
{
    /// <summary>CPF válido conhecido (dígitos verificadores corretos).</summary>
    private const string ValidDigits = "39053344705";

    [Fact]
    public void Create_accepts_digits_only()
    {
        var cpf = Cpf.Create(ValidDigits);
        cpf.Digits.Should().Be(ValidDigits);
    }

    [Fact]
    public void Create_accepts_formatted_input()
    {
        var cpf = Cpf.Create("390.533.447-05");
        cpf.Digits.Should().Be(ValidDigits);
    }

    [Fact]
    public void Create_throws_when_all_digits_equal()
    {
        var act = () => Cpf.Create("11111111111");
        act.Should().Throw<ArgumentException>().Which.Message.Should().Contain("invalid");
    }

    [Fact]
    public void Create_throws_when_check_digits_wrong()
    {
        var act = () => Cpf.Create("39053344700");
        act.Should().Throw<ArgumentException>().Which.Message.Should().Contain("check");
    }

    [Fact]
    public void Create_throws_when_too_short()
    {
        var act = () => Cpf.Create("1234567890");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_throws_when_empty()
    {
        var act = () => Cpf.Create("   ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToFormattedString_returns_mask()
    {
        var cpf = Cpf.Create(ValidDigits);
        cpf.ToFormattedString().Should().Be("390.533.447-05");
    }
}
