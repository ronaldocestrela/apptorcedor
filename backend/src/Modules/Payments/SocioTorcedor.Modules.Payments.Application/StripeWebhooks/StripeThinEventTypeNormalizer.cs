namespace SocioTorcedor.Modules.Payments.Application.StripeWebhooks;

/// <summary>
/// Normaliza tipos de thin events (<c>v1.invoice.paid</c>) para o formato dos handlers snapshot (<c>invoice.paid</c>).
/// </summary>
public static class StripeThinEventTypeNormalizer
{
    public static string Normalize(string? thinOrSnapshotType)
    {
        if (string.IsNullOrWhiteSpace(thinOrSnapshotType))
            return string.Empty;

        var t = thinOrSnapshotType.Trim();
        if (t.StartsWith("v1.", StringComparison.OrdinalIgnoreCase))
            return t[3..];

        return t;
    }
}
