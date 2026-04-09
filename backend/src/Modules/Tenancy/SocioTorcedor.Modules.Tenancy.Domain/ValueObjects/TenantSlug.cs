using System.Text.RegularExpressions;

namespace SocioTorcedor.Modules.Tenancy.Domain.ValueObjects;

public sealed partial class TenantSlug : IEquatable<TenantSlug>
{
    private static readonly Regex ValidPattern = MyRegex();

    private TenantSlug(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static TenantSlug Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Tenant slug is required.", nameof(raw));

        var normalized = raw.Trim().ToLowerInvariant();
        if (normalized.Length is < 2 or > 63)
            throw new ArgumentException("Tenant slug length must be between 2 and 63.", nameof(raw));

        if (!ValidPattern.IsMatch(normalized))
            throw new ArgumentException("Tenant slug must be lowercase alphanumeric with single hyphens.", nameof(raw));

        return new TenantSlug(normalized);
    }

    public bool Equals(TenantSlug? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is TenantSlug s && Equals(s);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;

    [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
