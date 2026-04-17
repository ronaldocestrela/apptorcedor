using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Options;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace AppTorcedor.Infrastructure.Services.Payments;

public enum StripeWebhookProcessResult
{
    Ok,
    BadSignature,
    ConfigurationError,
    IgnoredEventType,
    InvalidPayload,
}

public interface IStripeWebhookProcessor
{
    Task<StripeWebhookProcessResult> ProcessAsync(
        string json,
        string stripeSignatureHeader,
        CancellationToken cancellationToken = default);

    /// <summary>For tests: process a verified <see cref="Event"/> (no signature check).</summary>
    Task<StripeWebhookProcessResult> ProcessVerifiedEventAsync(Event stripeEvent, CancellationToken cancellationToken = default);
}

public sealed class StripeWebhookProcessor(
    AppDbContext db,
    ITorcedorSubscriptionCheckoutPort checkout,
    IOptions<PaymentsOptions> options,
    ILogger<StripeWebhookProcessor> logger) : IStripeWebhookProcessor
{
    public async Task<StripeWebhookProcessResult> ProcessAsync(
        string json,
        string stripeSignatureHeader,
        CancellationToken cancellationToken = default)
    {
        var whsec = options.Value.Stripe.WebhookSecret?.Trim();
        if (string.IsNullOrEmpty(whsec))
        {
            logger.LogWarning("Stripe webhook received but Payments:Stripe:WebhookSecret is empty.");
            return StripeWebhookProcessResult.ConfigurationError;
        }

        if (string.IsNullOrEmpty(stripeSignatureHeader))
        {
            logger.LogWarning("Stripe webhook missing Stripe-Signature header (proxy may have stripped it).");
            return StripeWebhookProcessResult.BadSignature;
        }

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(json, stripeSignatureHeader, whsec, throwOnApiVersionMismatch: false);
            return await ProcessVerifiedEventAsync(stripeEvent, cancellationToken).ConfigureAwait(false);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Stripe webhook signature validation failed.");
            return StripeWebhookProcessResult.BadSignature;
        }
    }

    public async Task<StripeWebhookProcessResult> ProcessVerifiedEventAsync(
        Event stripeEvent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(stripeEvent.Id))
        {
            logger.LogWarning("Stripe webhook event has empty Id after signature verification.");
            return StripeWebhookProcessResult.InvalidPayload;
        }

        if (await db.ProcessedStripeWebhookEvents.AsNoTracking()
                .AnyAsync(e => e.EventId == stripeEvent.Id, cancellationToken)
                .ConfigureAwait(false))
            return StripeWebhookProcessResult.Ok;

        if (!string.Equals(stripeEvent.Type, EventTypes.CheckoutSessionCompleted, StringComparison.Ordinal))
            return StripeWebhookProcessResult.IgnoredEventType;

        if (stripeEvent.Data.Object is not Session session)
        {
            logger.LogWarning(
                "Stripe checkout.session.completed has unexpected payload type (expected Session). EventType={EventType}.",
                stripeEvent.Type);
            return StripeWebhookProcessResult.InvalidPayload;
        }

        if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
            return StripeWebhookProcessResult.IgnoredEventType;

        if (session.Metadata is null
            || !session.Metadata.TryGetValue("payment_id", out var paymentIdStr)
            || !Guid.TryParse(paymentIdStr, out var paymentId))
        {
            logger.LogWarning("Stripe checkout.session.completed missing payment_id metadata.");
            return StripeWebhookProcessResult.InvalidPayload;
        }

        var payment = await db.Payments.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken)
            .ConfigureAwait(false);
        if (payment is null)
        {
            logger.LogWarning(
                "Stripe webhook payment_id {PaymentId} not found in database (wrong DB/homolog instance or stale metadata).",
                paymentId);
            return StripeWebhookProcessResult.InvalidPayload;
        }

        if (!string.Equals(payment.ProviderName, "Stripe", StringComparison.Ordinal))
        {
            logger.LogWarning("Stripe webhook for payment {PaymentId} but ProviderName is {Provider}.", paymentId, payment.ProviderName);
            return StripeWebhookProcessResult.InvalidPayload;
        }

        var expectedCents = (long)Math.Round(payment.Amount * 100m, MidpointRounding.AwayFromZero);
        var total = session.AmountTotal ?? 0;
        if (total != expectedCents
            || !string.Equals(session.Currency, "brl", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Stripe session totals mismatch for payment {PaymentId}: expected {ExpectedCents} BRL centavos, got {Actual} {Cur}.",
                paymentId,
                expectedCents,
                total,
                session.Currency);
            return StripeWebhookProcessResult.InvalidPayload;
        }

        var pi = session.PaymentIntentId;
        var confirm = await checkout
            .ConfirmPaymentAfterProviderSuccessAsync(paymentId, string.IsNullOrEmpty(pi) ? null : pi, cancellationToken)
            .ConfigureAwait(false);
        if (!confirm.Ok)
        {
            logger.LogWarning(
                "ConfirmPaymentAfterProviderSuccess failed for payment {PaymentId}: {Error}.",
                paymentId,
                confirm.Error);
            return StripeWebhookProcessResult.InvalidPayload;
        }

        try
        {
            db.ProcessedStripeWebhookEvents.Add(
                new ProcessedStripeWebhookEventRecord
                {
                    EventId = stripeEvent.Id,
                    EventType = stripeEvent.Type ?? EventTypes.CheckoutSessionCompleted,
                    ProcessedAtUtc = DateTimeOffset.UtcNow,
                    RelatedPaymentId = paymentId,
                });
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            if (await db.ProcessedStripeWebhookEvents.AsNoTracking()
                    .AnyAsync(e => e.EventId == stripeEvent.Id, cancellationToken)
                    .ConfigureAwait(false))
                return StripeWebhookProcessResult.Ok;

            logger.LogError(ex, "Failed to record Stripe webhook idempotency for {EventId}.", stripeEvent.Id);
            throw;
        }

        return StripeWebhookProcessResult.Ok;
    }
}
