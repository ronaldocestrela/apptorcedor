using FluentAssertions;
using SocioTorcedor.Modules.Payments.Application.Validation;

namespace SocioTorcedor.Modules.Payments.Application.Tests.Validation;

public sealed class CheckoutReturnUrlValidatorTests
{
    [Theory]
    [InlineData("https://app.example.com/member/billing?checkout=success")]
    [InlineData("http://localhost:5173/member/billing")]
    [InlineData("https://feira.localhost:5173/member/billing?checkout=cancel")]
    public void IsValid_accepts_http_https_absolute(string url)
    {
        var ok = CheckoutReturnUrlValidator.IsValid(url, out var err);
        ok.Should().BeTrue();
        err.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("relative/path")]
    [InlineData("/member/billing")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,hi")]
    public void IsValid_rejects_invalid(string url)
    {
        var ok = CheckoutReturnUrlValidator.IsValid(url, out var err);
        ok.Should().BeFalse();
        err.Should().NotBeNullOrEmpty();
    }
}
