using SocioTorcedor.BuildingBlocks.Domain.Abstractions;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Domain.Entities;

/// <summary>
/// Inbox idempotente para webhooks Stripe Connect (master).
/// </summary>
public sealed class ConnectStripeWebhookInbox : AggregateRoot
{
    private ConnectStripeWebhookInbox()
    {
    }

    public string IdempotencyKey { get; private set; } = null!;

    public string EventType { get; private set; } = null!;

    public string RawPayload { get; private set; } = null!;

    public WebhookInboxStatus Status { get; private set; }

    public DateTime? ProcessedAtUtc { get; private set; }

    public string? LastError { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static ConnectStripeWebhookInbox Receive(string idempotencyKey, string eventType, string rawPayload)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));

        return new ConnectStripeWebhookInbox
        {
            IdempotencyKey = idempotencyKey.Trim(),
            EventType = eventType ?? string.Empty,
            RawPayload = rawPayload ?? string.Empty,
            Status = WebhookInboxStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void MarkProcessed()
    {
        Status = WebhookInboxStatus.Processed;
        ProcessedAtUtc = DateTime.UtcNow;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        Status = WebhookInboxStatus.Failed;
        ProcessedAtUtc = DateTime.UtcNow;
        LastError = error;
    }
}
