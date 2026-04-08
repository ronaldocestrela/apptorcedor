namespace SocioTorcedor.Modules.Tenancy.Infrastructure.Services;

public static class SubdomainParser
{
    /// <summary>
    /// Extracts tenant subdomain from host (e.g. flamengo.meusistema.com -> flamengo).
    /// Returns null for apex hosts (e.g. meusistema.com) or localhost without subdomain.
    /// </summary>
    public static string? TryExtractSubdomain(string? hostHeader)
    {
        if (string.IsNullOrWhiteSpace(hostHeader))
            return null;

        var hostValue = hostHeader.Trim();
        var colon = hostValue.IndexOf(':');
        if (colon >= 0)
            hostValue = hostValue[..colon];
        if (string.IsNullOrEmpty(hostValue))
            return null;

        var parts = hostValue.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
            return null;

        var idx = 0;
        if (parts[0].Equals("www", StringComparison.OrdinalIgnoreCase))
        {
            idx++;
            if (idx >= parts.Length - 1)
                return null;
        }

        if (parts.Length - idx < 2)
            return null;

        return parts[idx];
    }
}
