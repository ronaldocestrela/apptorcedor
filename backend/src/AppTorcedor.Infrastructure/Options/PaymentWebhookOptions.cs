namespace AppTorcedor.Infrastructure.Options;

/// <summary>Shared secret for <c>POST /api/subscriptions/payments/callback</c> (mock / gateway webhooks).</summary>
public sealed class PaymentWebhookOptions
{
    public const string SectionName = "Payments";

    /// <summary>When empty, callback rejects all requests (except tests that set a value).</summary>
    public string WebhookSecret { get; set; } = string.Empty;
}
