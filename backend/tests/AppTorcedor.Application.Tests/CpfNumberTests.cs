using AppTorcedor.Application.Validation;
using Xunit;

namespace AppTorcedor.Application.Tests;

public sealed class CpfNumberTests
{
    [Theory]
    [InlineData("11144477735", "11144477735")]
    [InlineData("39053344705", "39053344705")]
    [InlineData("111.444.777-35", "11144477735")]
    [InlineData(" 11144477735 ", "11144477735")]
    [InlineData("111 444 777 35", "11144477735")]
    public void TryParse_accepts_valid_with_or_without_mask(string input, string expected)
    {
        Assert.True(CpfNumber.TryParse(input, out var norm));
        Assert.Equal(expected, norm);
    }

    [Theory]
    [InlineData("12345678901")]
    [InlineData("00000000000")]
    [InlineData("11111111111")]
    [InlineData("12")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1234567890123")]
    public void TryParse_rejects_invalid(string? input)
    {
        Assert.False(CpfNumber.TryParse(input, out _));
    }
}
