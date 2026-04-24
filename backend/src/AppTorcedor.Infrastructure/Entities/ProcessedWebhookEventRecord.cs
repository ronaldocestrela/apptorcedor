namespace AppTorcedor.Infrastructure.Entities;

/// <summary>Idempotência de webhooks de pagamento (<c>Stripe</c>, <c>Asaas</c>, etc.).</summary>
public sealed class ProcessedWebhookEventRecord
{
    public string EventId { get; set; } = string.Empty;

    /// <summary>Provedor que originou o evento (ex.: Stripe, Asaas).</summary>
    public string Provider { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public DateTimeOffset ProcessedAtUtc { get; set; }

    public Guid? RelatedPaymentId { get; set; }
}
