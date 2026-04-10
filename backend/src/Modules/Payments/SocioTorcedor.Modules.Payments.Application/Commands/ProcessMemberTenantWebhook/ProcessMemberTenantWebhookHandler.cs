using System.Text.Json;
using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Domain.Enums;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Domain.Entities;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Application.Commands.ProcessMemberTenantWebhook;

public sealed class ProcessMemberTenantWebhookHandler(
    IMemberTenantPaymentsRepository paymentsRepository,
    IMemberProfileRepository memberProfileRepository)
    : ICommandHandler<ProcessMemberTenantWebhookCommand>
{
    public async Task<Result> Handle(ProcessMemberTenantWebhookCommand command, CancellationToken cancellationToken)
    {
        var inbox = await paymentsRepository.GetWebhookByIdempotencyKeyAsync(command.IdempotencyKey, cancellationToken);
        if (inbox is null)
        {
            inbox = MemberPaymentWebhookInbox.Receive(command.IdempotencyKey, command.EventType, command.RawBody);
            await paymentsRepository.AddWebhookAsync(inbox, cancellationToken);
            await paymentsRepository.SaveChangesAsync(cancellationToken);
        }
        else if (inbox.Status == WebhookInboxStatus.Processed)
        {
            return Result.Ok();
        }

        try
        {
            await ApplyEffectsAsync(command.RawBody, cancellationToken);
            inbox.MarkProcessed();
            await paymentsRepository.SaveChangesAsync(cancellationToken);
            await memberProfileRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            inbox.MarkFailed(ex.Message);
            await paymentsRepository.SaveChangesAsync(cancellationToken);
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
        var memberIdStr = root.TryGetProperty("memberProfileId", out var mp) ? mp.GetString() : null;

        var isPaid = string.Equals(eventType, "invoice.paid", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(eventType, "payment.confirmed", StringComparison.OrdinalIgnoreCase);

        var isFailed = string.Equals(eventType, "invoice.payment_failed", StringComparison.OrdinalIgnoreCase);

        if (isPaid && !string.IsNullOrWhiteSpace(externalSub))
        {
            var subscription = await paymentsRepository.GetSubscriptionByExternalIdAsync(externalSub, cancellationToken);
            if (subscription is null)
                return;

            var invoices = await paymentsRepository.ListInvoicesByMemberAsync(subscription.MemberProfileId, 0, 50, cancellationToken);
            var open = invoices.FirstOrDefault(i => i.Status is BillingInvoiceStatus.Open or BillingInvoiceStatus.Draft);
            open?.MarkPaid(DateTime.UtcNow);

            subscription.MarkStatus(BillingSubscriptionStatus.Active, DateTime.UtcNow.AddMonths(1));

            var profile = await memberProfileRepository.GetTrackedByIdAsync(subscription.MemberProfileId, cancellationToken);
            if (profile is not null && profile.Status == MemberStatus.Delinquent)
                profile.ChangeStatus(MemberStatus.Active);

            return;
        }

        if (isFailed)
        {
            MemberBillingSubscription? subscription = null;
            if (!string.IsNullOrWhiteSpace(externalSub))
                subscription = await paymentsRepository.GetSubscriptionByExternalIdAsync(externalSub, cancellationToken);

            if (subscription is null && Guid.TryParse(memberIdStr, out var memberId))
                subscription = await paymentsRepository.GetActiveSubscriptionByMemberAsync(memberId, cancellationToken);

            if (subscription is null)
                return;

            subscription.MarkStatus(BillingSubscriptionStatus.PastDue);

            var profile = await memberProfileRepository.GetTrackedByIdAsync(subscription.MemberProfileId, cancellationToken);
            if (profile is not null && profile.Status == MemberStatus.Active)
                profile.ChangeStatus(MemberStatus.Delinquent);
        }
    }
}
