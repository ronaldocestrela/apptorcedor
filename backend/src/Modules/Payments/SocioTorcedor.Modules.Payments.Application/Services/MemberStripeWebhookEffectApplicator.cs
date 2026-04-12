using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.Options;
using SocioTorcedor.Modules.Payments.Domain.Entities;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Application.Services;

public sealed class MemberStripeWebhookEffectApplicator(
    ITenantConnectionStringResolver connectionStringResolver,
    IMemberTenantPaymentsScopeFactory tenantPaymentsScopeFactory,
    IMemberProfileStatusService memberProfileStatusService,
    IOptions<StripeWebhookHandlingOptions> webhookBehavior,
    ILogger<MemberStripeWebhookEffectApplicator> logger)
    : IMemberStripeWebhookEffectApplicator
{
    public async Task ApplyAsync(string eventType, string rawJson, CancellationToken cancellationToken)
    {
        if (webhookBehavior.Value.StripeWebhookShadowMode)
        {
            logger.LogInformation(
                "Stripe member webhook shadow mode: skipping domain effects (type {EventType}, body length {Length}).",
                eventType,
                rawJson?.Length ?? 0);
            return;
        }

        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(rawJson) ? "{}" : rawJson);
        var root = doc.RootElement;
        if (!root.TryGetProperty("data", out var data) || !data.TryGetProperty("object", out var obj))
            return;

        switch (eventType)
        {
            case "checkout.session.completed":
                await HandleCheckoutSessionCompletedAsync(obj, cancellationToken);
                break;
            case "customer.subscription.updated":
            case "customer.subscription.deleted":
                await HandleMemberSubscriptionAsync(eventType, obj, cancellationToken);
                break;
            case "invoice.payment_failed":
            case "invoice.paid":
                await HandleMemberInvoiceAsync(eventType, obj, cancellationToken);
                break;
        }
    }

    private async Task HandleCheckoutSessionCompletedAsync(JsonElement session, CancellationToken cancellationToken)
    {
        var mode = GetString(session, "mode");
        if (!string.Equals(mode, "subscription", StringComparison.OrdinalIgnoreCase))
            return;

        if (!session.TryGetProperty("metadata", out var meta))
            return;

        var tenantId = GetMetaGuid(meta, "tenant_id");
        var memberId = GetMetaGuid(meta, "member_profile_id");
        var planId = GetMetaGuid(meta, "member_plan_id");
        if (tenantId is null || memberId is null || planId is null)
            return;

        var cs = await connectionStringResolver.GetConnectionStringAsync(tenantId.Value, cancellationToken);
        if (string.IsNullOrWhiteSpace(cs))
            return;

        var customerId = GetString(session, "customer");
        var subscriptionId = GetString(session, "subscription");
        if (string.IsNullOrWhiteSpace(subscriptionId))
            return;

        await using var scope = tenantPaymentsScopeFactory.Create(cs);
        var repo = scope.Repository;

        var existing = await repo.GetActiveSubscriptionByMemberAsync(memberId.Value, cancellationToken);
        if (existing is not null)
            existing.MarkStatus(BillingSubscriptionStatus.Canceled);

        var amount = existing?.RecurringAmount ?? 0m;
        if (amount <= 0m && meta.TryGetProperty("recurring_amount_brl", out var amountEl) &&
            amountEl.ValueKind == JsonValueKind.String &&
            decimal.TryParse(
                amountEl.GetString(),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var parsedAmount))
        {
            amount = parsedAmount;
        }

        if (amount <= 0m && session.TryGetProperty("amount_total", out var amountTotal) &&
            amountTotal.ValueKind == JsonValueKind.Number)
        {
            // Stripe amounts are in minor units (centavos for BRL).
            amount = amountTotal.GetInt64() / 100m;
        }

        var sub = MemberBillingSubscription.Start(
            memberId.Value,
            planId.Value,
            amount,
            "BRL",
            PaymentMethodKind.Card,
            customerId,
            subscriptionId,
            BillingSubscriptionStatus.Active,
            DateTime.UtcNow.AddMonths(1));

        await repo.AddSubscriptionAsync(sub, cancellationToken);

        var invoice = MemberBillingInvoice.Create(
            sub.Id,
            amount,
            "BRL",
            PaymentMethodKind.Card,
            DateTime.UtcNow.AddDays(7),
            BillingInvoiceStatus.Open,
            externalInvoiceId: null,
            pixCopyPaste: null);
        invoice.MarkPaid(DateTime.UtcNow);

        await repo.AddInvoiceAsync(invoice, cancellationToken);
        await repo.SaveChangesAsync(cancellationToken);

        await memberProfileStatusService.TrySetActiveAsync(cs, memberId.Value, cancellationToken);
    }

    private async Task HandleMemberSubscriptionAsync(string eventType, JsonElement subObj, CancellationToken cancellationToken)
    {
        var subscriptionId = GetString(subObj, "id");
        if (string.IsNullOrWhiteSpace(subscriptionId))
            return;

        Guid? tenantId = null;
        if (subObj.TryGetProperty("metadata", out var meta))
            tenantId = GetMetaGuid(meta, "tenant_id");

        if (tenantId is null)
            return;

        var cs = await connectionStringResolver.GetConnectionStringAsync(tenantId.Value, cancellationToken);
        if (string.IsNullOrWhiteSpace(cs))
            return;

        await using var scope = tenantPaymentsScopeFactory.Create(cs);
        var repo = scope.Repository;
        var subscription = await repo.GetSubscriptionByExternalIdAsync(subscriptionId, cancellationToken);
        if (subscription is null)
            return;

        if (string.Equals(eventType, "customer.subscription.deleted", StringComparison.OrdinalIgnoreCase))
        {
            subscription.MarkStatus(BillingSubscriptionStatus.Canceled);
            await repo.SaveChangesAsync(cancellationToken);
            return;
        }

        var status = GetString(subObj, "status");
        if (string.Equals(status, "past_due", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "unpaid", StringComparison.OrdinalIgnoreCase))
        {
            subscription.MarkStatus(BillingSubscriptionStatus.PastDue);
            await repo.SaveChangesAsync(cancellationToken);
            await memberProfileStatusService.TrySetDelinquentAsync(cs, subscription.MemberProfileId, cancellationToken);
            return;
        }

        if (string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
        {
            subscription.MarkStatus(BillingSubscriptionStatus.Active, DateTime.UtcNow.AddMonths(1));
            await repo.SaveChangesAsync(cancellationToken);
            await memberProfileStatusService.TrySetActiveAsync(cs, subscription.MemberProfileId, cancellationToken);
        }
    }

    private async Task HandleMemberInvoiceAsync(string eventType, JsonElement invoice, CancellationToken cancellationToken)
    {
        var subscriptionId = GetString(invoice, "subscription");
        if (string.IsNullOrWhiteSpace(subscriptionId))
            return;

        if (!invoice.TryGetProperty("metadata", out var meta))
            return;

        var tenantId = GetMetaGuid(meta, "tenant_id");
        if (tenantId is null)
            return;

        var cs = await connectionStringResolver.GetConnectionStringAsync(tenantId.Value, cancellationToken);
        if (string.IsNullOrWhiteSpace(cs))
            return;

        await using var scope = tenantPaymentsScopeFactory.Create(cs);
        var repo = scope.Repository;
        var subscription = await repo.GetSubscriptionByExternalIdAsync(subscriptionId, cancellationToken);
        if (subscription is null)
            return;

        if (string.Equals(eventType, "invoice.paid", StringComparison.OrdinalIgnoreCase))
        {
            var invoices = await repo.ListInvoicesByMemberAsync(subscription.MemberProfileId, 0, 50, cancellationToken);
            var open = invoices.FirstOrDefault(i => i.Status is BillingInvoiceStatus.Open or BillingInvoiceStatus.Draft);
            open?.MarkPaid(DateTime.UtcNow);
            subscription.MarkStatus(BillingSubscriptionStatus.Active, DateTime.UtcNow.AddMonths(1));
            await repo.SaveChangesAsync(cancellationToken);
            await memberProfileStatusService.TrySetActiveAsync(cs, subscription.MemberProfileId, cancellationToken);
        }
        else if (string.Equals(eventType, "invoice.payment_failed", StringComparison.OrdinalIgnoreCase))
        {
            subscription.MarkStatus(BillingSubscriptionStatus.PastDue);
            await repo.SaveChangesAsync(cancellationToken);
            await memberProfileStatusService.TrySetDelinquentAsync(cs, subscription.MemberProfileId, cancellationToken);
        }
    }

    private static string? GetString(JsonElement el, string name) =>
        el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    private static Guid? GetMetaGuid(JsonElement meta, string key)
    {
        if (!meta.TryGetProperty(key, out var p) || p.ValueKind != JsonValueKind.String)
            return null;
        return Guid.TryParse(p.GetString(), out var g) ? g : null;
    }
}
