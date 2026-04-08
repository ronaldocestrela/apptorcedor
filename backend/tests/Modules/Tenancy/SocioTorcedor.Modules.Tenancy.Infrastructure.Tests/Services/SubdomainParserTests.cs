using FluentAssertions;
using SocioTorcedor.Modules.Tenancy.Infrastructure.Services;

namespace SocioTorcedor.Modules.Tenancy.Infrastructure.Tests.Services;

public class SubdomainParserTests
{
    [Theory]
    [InlineData("flamengo.meusistema.com", "flamengo")]
    [InlineData("ffc.localhost:8080", "ffc")]
    [InlineData("www.ffc.meusistema.com", "ffc")]
    public void TryExtractSubdomain_parses_host(string host, string expected)
    {
        SubdomainParser.TryExtractSubdomain(host).Should().Be(expected);
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("localhost:5000")]
    [InlineData("")]
    [InlineData(null)]
    public void TryExtractSubdomain_returns_null_for_apex_or_missing(string? host)
    {
        SubdomainParser.TryExtractSubdomain(host).Should().BeNull();
    }
}
