using System.Text.Json;
using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Backoffice.Domain.Enums;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Domain.Entities;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Application.Commands.ProcessTenantSaasWebhook;

/// <summary>
/// Persiste o webhook em inbox (idempotente) e aplica efeitos (legado + eventos Stripe nativos).
/// </summary>
public sealed class ProcessTenantSaasWebhookHandler(ITenantMasterPaymentsRepository repository)
    : ICommandHandler<ProcessTenantSaasWebhookCommand>
{
    public async Task<Result> Handle(ProcessTenantSaasWebhookCommand command, CancellationToken cancellationToken)
    {
        var inbox = await repository.GetWebhookByIdempotencyKeyAsync(command.IdempotencyKey, cancellationToken);
        if (inbox is null)
        {
            inbox = TenantPaymentWebhookInbox.Receive(command.IdempotencyKey, command.EventType, command.RawBody);
            await repository.AddWebhookAsync(inbox, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
        }
        else if (inbox.Status == WebhookInboxStatus.Processed)
        {
            return Result.Ok();
        }

        try
        {
            await ApplyEffectsAsync(command.RawBody, cancellationToken);
            inbox.MarkProcessed();
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            inbox.MarkFailed(ex.Message);
            await repository.SaveChangesAsync(cancellationToken);
            return Result.Fail(Error.Failure("Payments.Webhook.ProcessFailed", ex.Message));
        }

        return Result.Ok();
    }

    private async Task ApplyEffectsAsync(string rawBody, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(rawBody) ? "{}" : rawBody);
        var root = doc.RootElement;

        if (root.TryGetProperty("type", out var stripeTypeEl) && root.TryGetProperty("data", out var dataEl))
        {
            await ApplyStripeEventAsync(stripeTypeEl.GetString() ?? string.Empty, dataEl, cancellationToken);
            return;
        }

        var eventType = root.TryGetProperty("eventType", out var et) ? et.GetString() : null;
        var externalSub = root.TryGetProperty("externalSubscriptionId", out var es) ? es.GetString() : null;

        var isPaid = string.Equals(eventType, "invoice.paid", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(eventType, "payment.confirmed", StringComparison.OrdinalIgnoreCase);

        if (!isPaid || string.IsNullOrWhiteSpace(externalSub))
            return;

        var subscription = await repository.GetSubscriptionByExternalIdAsync(externalSub, cancellationToken);
        if (subscription is null)
            return;

        var invoices = await repository.ListInvoicesByTenantAsync(subscription.TenantId, 0, 50, cancellationToken);
        var open = invoices.FirstOrDefault(i => i.Status is BillingInvoiceStatus.Open or BillingInvoiceStatus.Draft);
        open?.MarkPaid(DateTime.UtcNow);

        var next = subscription.BillingCycle == BillingCycle.Yearly
            ? DateTime.UtcNow.AddYears(1)
            : DateTime.UtcNow.AddMonths(1);
        subscription.MarkStatus(BillingSubscriptionStatus.Active, next);
    }

    private async Task ApplyStripeEventAsync(string type, JsonElement data, CancellationToken cancellationToken)
    {
        if (!data.TryGetProperty("object", out var obj))
            return;

        switch (type)
        {
            case "invoice.paid":
                await HandleInvoicePaidAsync(obj, cancellationToken);
                break;
            case "invoice.payment_failed":
                await HandleInvoicePaymentFailedAsync(obj, cancellationToken);
                break;
            case "customer.subscription.updated":
                await HandleSubscriptionUpdatedAsync(obj, cancellationToken);
                break;
            case "customer.subscription.deleted":
                await HandleSubscriptionDeletedAsync(obj, cancellationToken);
                break;
        }
    }

    private async Task HandleInvoicePaidAsync(JsonElement invoice, CancellationToken cancellationToken)
    {
        var subId = GetStringOrNull(invoice, "subscription");
        if (string.IsNullOrWhiteSpace(subId))
            return;

        var subscription = await repository.GetSubscriptionByExternalIdAsync(subId, cancellationToken);
        if (subscription is null)
            return;

        var invoices = await repository.ListInvoicesByTenantAsync(subscription.TenantId, 0, 50, cancellationToken);
        var open = invoices.FirstOrDefault(i => i.Status is BillingInvoiceStatus.Open or BillingInvoiceStatus.Draft);
        open?.MarkPaid(DateTime.UtcNow);

        var next = subscription.BillingCycle == BillingCycle.Yearly
            ? DateTime.UtcNow.AddYears(1)
            : DateTime.UtcNow.AddMonths(1);
        subscription.MarkStatus(BillingSubscriptionStatus.Active, next);
    }

    private async Task HandleInvoicePaymentFailedAsync(JsonElement invoice, CancellationToken cancellationToken)
    {
        var subId = GetStringOrNull(invoice, "subscription");
        if (string.IsNullOrWhiteSpace(subId))
            return;

        var subscription = await repository.GetSubscriptionByExternalIdAsync(subId, cancellationToken);
        if (subscription is null)
            return;

        subscription.MarkStatus(BillingSubscriptionStatus.PastDue);
    }

    private async Task HandleSubscriptionUpdatedAsync(JsonElement subObj, CancellationToken cancellationToken)
    {
        var subId = GetStringOrNull(subObj, "id");
        if (string.IsNullOrWhiteSpace(subId))
            return;

        var subscription = await repository.GetSubscriptionByExternalIdAsync(subId, cancellationToken);
        if (subscription is null)
            return;

        var status = GetStringOrNull(subObj, "status");
        if (string.Equals(status, "canceled", StringComparison.OrdinalIgnoreCase))
        {
            subscription.MarkStatus(BillingSubscriptionStatus.Canceled);
            return;
        }

        if (string.Equals(status, "past_due", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "unpaid", StringComparison.OrdinalIgnoreCase))
        {
            subscription.MarkStatus(BillingSubscriptionStatus.PastDue);
            return;
        }

        if (string.Equals(status, "active", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "trialing", StringComparison.OrdinalIgnoreCase))
        {
            if (subObj.TryGetProperty("current_period_end", out var cpe) && cpe.ValueKind == JsonValueKind.Number)
            {
                var end = DateTimeOffset.FromUnixTimeSeconds(cpe.GetInt64()).UtcDateTime;
                subscription.SetStripeBillingMetadata(subscription.StripePriceId, end);
            }

            subscription.MarkStatus(BillingSubscriptionStatus.Active);
        }
    }

    private async Task HandleSubscriptionDeletedAsync(JsonElement subObj, CancellationToken cancellationToken)
    {
        var subId = GetStringOrNull(subObj, "id");
        if (string.IsNullOrWhiteSpace(subId))
            return;

        var subscription = await repository.GetSubscriptionByExternalIdAsync(subId, cancellationToken);
        if (subscription is null)
            return;

        subscription.MarkStatus(BillingSubscriptionStatus.Canceled);
    }

    private static string? GetStringOrNull(JsonElement el, string name) =>
        el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
}
