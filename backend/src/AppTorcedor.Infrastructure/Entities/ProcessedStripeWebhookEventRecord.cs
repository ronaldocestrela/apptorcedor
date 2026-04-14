namespace AppTorcedor.Infrastructure.Entities;

/// <summary>Idempotency store for Stripe webhook <c>event.id</c> values.</summary>
public sealed class ProcessedStripeWebhookEventRecord
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAtUtc { get; set; }
    public Guid? RelatedPaymentId { get; set; }
}
