using Microsoft.Extensions.Configuration;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;

namespace SocioTorcedor.Modules.Tenancy.Infrastructure.Services;

public sealed class TenantAutoCorsOriginProvider(IConfiguration configuration) : ITenantAutoCorsOriginProvider
{
    public const string ConfigurationKey = "CORS_BASE_DOMAIN";

    public const string SlugPlaceholder = "{slug}";

    /// <summary>
    /// Host:porta do fallback dev (origem <c>http://{slug}.localhost:5173</c>).
    /// </summary>
    public const string DefaultLocalHostPort = "localhost:5173";

    public string GetDefaultOriginForNewTenant(string tenantSlug)
    {
        if (string.IsNullOrWhiteSpace(tenantSlug))
            throw new ArgumentException("Tenant slug is required.", nameof(tenantSlug));

        var slug = tenantSlug.Trim();
        var raw = configuration[ConfigurationKey];
        if (string.IsNullOrWhiteSpace(raw))
            return BuildSlugSubdomainOrigin("http", DefaultLocalHostPort, slug);

        var trimmed = raw.Trim();
        if (trimmed.Contains(SlugPlaceholder, StringComparison.Ordinal))
            return trimmed.Replace(SlugPlaceholder, slug, StringComparison.Ordinal).Trim().TrimEnd('/');

        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return trimmed.TrimEnd('/');

        var host = trimmed.TrimEnd('/');
        var scheme = host.Contains("localhost", StringComparison.OrdinalIgnoreCase) ? "http" : "https";
        return BuildSlugSubdomainOrigin(scheme, host, slug);
    }

    private static string BuildSlugSubdomainOrigin(string scheme, string hostPortOrHost, string slug) =>
        $"{scheme}://{slug}.{hostPortOrHost.Trim().TrimEnd('/')}";
}
