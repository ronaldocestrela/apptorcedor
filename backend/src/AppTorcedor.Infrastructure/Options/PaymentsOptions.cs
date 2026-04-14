namespace AppTorcedor.Infrastructure.Options;

/// <summary>Payment gateway configuration (mock vs Stripe, legacy callback secret, Stripe keys/URLs).</summary>
public sealed class PaymentsOptions
{
    public const string SectionName = "Payments";

    /// <summary>Mock (default) or Stripe.</summary>
    public string Provider { get; set; } = "Mock";

    /// <summary>Shared secret for <c>POST /api/subscriptions/payments/callback</c> (testing / legacy integrations).</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    public PaymentsStripeOptions Stripe { get; set; } = new();
}

/// <summary>Stripe Checkout + webhooks (cartão).</summary>
public sealed class PaymentsStripeOptions
{
    /// <summary>API secret key (sk_live_… / sk_test_…).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Signing secret for the Stripe webhook endpoint (whsec_…).</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>Success redirect after Checkout (HTTPS in production).</summary>
    public string SuccessUrl { get; set; } = string.Empty;

    /// <summary>Cancel redirect when the user abandons Checkout.</summary>
    public string CancelUrl { get; set; } = string.Empty;
}
