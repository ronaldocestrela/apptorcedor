using System.Text.Json;

namespace SocioTorcedor.Modules.Payments.Application.StripeWebhooks;

/// <summary>
/// Detecta envelopes de webhook Stripe (thin / Event Destination vs snapshot V1).
/// </summary>
public static class StripeWebhookEnvelope
{
    public const string ThinEventObjectType = "v2.core.event";

    /// <summary>
    /// Thin events (Event Destinations) usam <c>object: v2.core.event</c>.
    /// </summary>
    public static bool IsThinEventNotification(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("object", out var o)
                && string.Equals(o.GetString(), ThinEventObjectType, StringComparison.Ordinal);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
