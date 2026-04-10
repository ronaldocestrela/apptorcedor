using System.Text.Json;
using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Domain.Entities;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Application.Commands.ProcessTenantSaasWebhook;

/// <summary>
/// Persiste o webhook em inbox (idempotente) e aplica efeitos mínimos (ex.: fatura paga) para o MVP.
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

        var next = subscription.BillingCycle == Backoffice.Domain.Enums.BillingCycle.Yearly
            ? DateTime.UtcNow.AddYears(1)
            : DateTime.UtcNow.AddMonths(1);
        subscription.MarkStatus(BillingSubscriptionStatus.Active, next);
    }
}
