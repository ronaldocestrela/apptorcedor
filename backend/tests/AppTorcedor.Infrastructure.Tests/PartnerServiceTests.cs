using AppTorcedor.Infrastructure.Services.Partner;
using Xunit;

namespace AppTorcedor.Infrastructure.Tests;

public sealed class PartnerPhoneNormalizationTests
{
    [Theory]
    [InlineData("11999999999", "11999999999")]
    [InlineData("(11) 99999-9999", "11999999999")]
    [InlineData("+55 (11) 99999-9999", "5511999999999")]
    [InlineData("55 11 99999-9999", "5511999999999")]
    [InlineData("  11999999999  ", "11999999999")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void NormalizePhone_returns_only_digits(string input, string expected)
    {
        var result = PartnerLookupService.NormalizePhone(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("5511999999999", "11999999999")]   // 13 dígitos → remove 55
    [InlineData("551199999999", "1199999999")]     // 12 dígitos → remove 55
    [InlineData("11999999999", "11999999999")]     // 11 dígitos → sem alteração
    [InlineData("1199999999", "1199999999")]       // 10 dígitos → sem alteração
    public void StripCountryCode_removes_55_prefix_when_applicable(string digits, string expected)
    {
        var result = PartnerLookupService.StripCountryCode(digits);
        Assert.Equal(expected, result);
    }
}

public sealed class PartnerApiKeyHashTests
{
    [Fact]
    public void ComputeHash_returns_64_char_lowercase_hex()
    {
        var hash = PartnerApiKeyService.ComputeHash("sk_partner_test123");
        Assert.Equal(64, hash.Length);
        Assert.All(hash, c => Assert.True(char.IsAsciiHexDigitLower(c) || char.IsDigit(c)));
    }

    [Fact]
    public void ComputeHash_is_deterministic()
    {
        var key = "sk_partner_abc";
        Assert.Equal(PartnerApiKeyService.ComputeHash(key), PartnerApiKeyService.ComputeHash(key));
    }

    [Fact]
    public void ComputeHash_different_keys_produce_different_hashes()
    {
        var hash1 = PartnerApiKeyService.ComputeHash("sk_partner_key1");
        var hash2 = PartnerApiKeyService.ComputeHash("sk_partner_key2");
        Assert.NotEqual(hash1, hash2);
    }
}
