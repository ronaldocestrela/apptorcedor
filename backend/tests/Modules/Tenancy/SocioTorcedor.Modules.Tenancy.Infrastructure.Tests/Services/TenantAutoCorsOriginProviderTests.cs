using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using SocioTorcedor.Modules.Tenancy.Infrastructure.Services;

namespace SocioTorcedor.Modules.Tenancy.Infrastructure.Tests.Services;

public sealed class TenantAutoCorsOriginProviderTests
{
    private const string Slug = "feira";

    private static IConfiguration ConfigFromJson(string json)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return new ConfigurationBuilder().AddJsonStream(stream).Build();
    }

    [Fact]
    public void When_key_missing_uses_slug_subdomain_localhost_5173()
    {
        var sut = new TenantAutoCorsOriginProvider(ConfigFromJson("{}"));

        sut.GetDefaultOriginForNewTenant(Slug).Should().Be($"http://{Slug}.{TenantAutoCorsOriginProvider.DefaultLocalHostPort}");
    }

    [Fact]
    public void When_key_empty_uses_slug_subdomain_localhost_5173()
    {
        var json = $$"""{"KEY":"   "}""".Replace("KEY", TenantAutoCorsOriginProvider.ConfigurationKey);
        var sut = new TenantAutoCorsOriginProvider(ConfigFromJson(json));

        sut.GetDefaultOriginForNewTenant(Slug).Should().Be($"http://{Slug}.{TenantAutoCorsOriginProvider.DefaultLocalHostPort}");
    }

    [Fact]
    public void When_key_is_full_url_without_placeholder_uses_as_is()
    {
        var json = $$"""{"KEY":"  https://app.example.com/  "}""".Replace(
            "KEY",
            TenantAutoCorsOriginProvider.ConfigurationKey);
        var sut = new TenantAutoCorsOriginProvider(ConfigFromJson(json));

        sut.GetDefaultOriginForNewTenant(Slug).Should().Be("https://app.example.com");
    }

    [Fact]
    public void When_key_contains_slug_placeholder_replaces()
    {
        var json = $$"""{"KEY":"https://{slug}.myapp.com"}""".Replace(
            "KEY",
            TenantAutoCorsOriginProvider.ConfigurationKey);
        var sut = new TenantAutoCorsOriginProvider(ConfigFromJson(json));

        sut.GetDefaultOriginForNewTenant(Slug).Should().Be($"https://{Slug}.myapp.com");
    }

    [Fact]
    public void When_key_is_bare_localhost_port_prefixes_slug()
    {
        var json = $$"""{"KEY":"localhost:5173"}""".Replace("KEY", TenantAutoCorsOriginProvider.ConfigurationKey);
        var sut = new TenantAutoCorsOriginProvider(ConfigFromJson(json));

        sut.GetDefaultOriginForNewTenant(Slug).Should().Be($"http://{Slug}.localhost:5173");
    }

    [Fact]
    public void When_key_is_bare_prod_host_uses_https_and_slug()
    {
        var json = $$"""{"KEY":"app.clube.com"}""".Replace("KEY", TenantAutoCorsOriginProvider.ConfigurationKey);
        var sut = new TenantAutoCorsOriginProvider(ConfigFromJson(json));

        sut.GetDefaultOriginForNewTenant(Slug).Should().Be($"https://{Slug}.app.clube.com");
    }

    [Fact]
    public void When_slug_empty_throws()
    {
        var sut = new TenantAutoCorsOriginProvider(ConfigFromJson("{}"));
        var act = () => sut.GetDefaultOriginForNewTenant("  ");
        act.Should().Throw<ArgumentException>().WithParameterName("tenantSlug");
    }
}
