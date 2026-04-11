using System.Text.Json;
using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Domain.Entities;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Application.Commands.ProcessStripeConnectWebhook;

public sealed class ProcessStripeConnectWebhookHandler(
    ITenantMasterPaymentsRepository masterPaymentsRepository,
    ITenantConnectionStringResolver connectionStringResolver,
    IMemberTenantPaymentsScopeFactory tenantPaymentsScopeFactory,
    IMemberProfileStatusService memberProfileStatusService)
    : ICommandHandler<ProcessStripeConnectWebhookCommand>
{
    public async Task<Result> Handle(ProcessStripeConnectWebhookCommand command, CancellationToken cancellationToken)
    {
        var inbox = await masterPaymentsRepository.GetConnectWebhookByIdempotencyKeyAsync(command.StripeEventId, cancellationToken);
        if (inbox is null)
        {
            inbox = ConnectStripeWebhookInbox.Receive(command.StripeEventId, command.EventType, command.RawJson);
            await masterPaymentsRepository.AddConnectWebhookAsync(inbox, cancellationToken);
            await masterPaymentsRepository.SaveChangesAsync(cancellationToken);
        }
        else if (inbox.Status == WebhookInboxStatus.Processed)
            return Result.Ok();

        try
        {
            await ApplyAsync(command.EventType, command.RawJson, cancellationToken);
            inbox.MarkProcessed();
            await masterPaymentsRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            inbox.MarkFailed(ex.Message);
            await masterPaymentsRepository.SaveChangesAsync(cancellationToken);
            return Result.Fail(Error.Failure("Payments.ConnectWebhook.ProcessFailed", ex.Message));
        }

        return Result.Ok();
    }

    private async Task ApplyAsync(string eventType, string rawJson, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(rawJson) ? "{}" : rawJson);
        var root = doc.RootElement;
        if (!root.TryGetProperty("data", out var data) || !data.TryGetProperty("object", out var obj))
            return;

        switch (eventType)
        {
            case "account.updated":
                await HandleAccountUpdatedAsync(obj, cancellationToken);
                break;
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

    private async Task HandleAccountUpdatedAsync(JsonElement account, CancellationToken cancellationToken)
    {
        var id = GetString(account, "id");
        if (string.IsNullOrWhiteSpace(id))
            return;

        var row = await masterPaymentsRepository.GetStripeConnectByStripeAccountIdAsync(id, cancellationToken);
        if (row is null)
            return;

        row.SyncFromStripe(
            account.TryGetProperty("charges_enabled", out var ce) && ce.GetBoolean(),
            account.TryGetProperty("payouts_enabled", out var pe) && pe.GetBoolean(),
            account.TryGetProperty("details_submitted", out var ds) && ds.GetBoolean());
        await masterPaymentsRepository.SaveChangesAsync(cancellationToken);
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
        {
            existing.MarkStatus(BillingSubscriptionStatus.Canceled);
        }

        // Valor: webhook não traz preço de forma estável; usar assinatura existente ou zero até sincronizar.
        var amount = existing?.RecurringAmount ?? 0m;
        var sub = MemberBillingSubscription.Start(
            memberId.Value,
            planId.Value,
            amount,
            "BRL",
            PaymentMethodKind.Unspecified,
            customerId,
            subscriptionId,
            BillingSubscriptionStatus.Active,
            DateTime.UtcNow.AddMonths(1));

        await repo.AddSubscriptionAsync(sub, cancellationToken);

        var invoice = MemberBillingInvoice.Create(
            sub.Id,
            amount,
            "BRL",
            PaymentMethodKind.Unspecified,
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
