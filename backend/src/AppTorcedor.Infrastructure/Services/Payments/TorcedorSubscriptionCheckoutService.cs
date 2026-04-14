using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Payments;
using AppTorcedor.Application.Modules.Torcedor.Commands.SubscribeMember;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Options;
using AppTorcedor.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AppTorcedor.Infrastructure.Services.Payments;

public sealed class TorcedorSubscriptionCheckoutService(
    IMediator mediator,
    AppDbContext db,
    IPaymentProvider paymentProvider,
    ILoyaltyPointsTriggerPort loyaltyPoints,
    IOptions<PaymentsOptions> paymentsOptions) : ITorcedorSubscriptionCheckoutPort
{
    private const string Currency = "BRL";

    public async Task<CreateTorcedorSubscriptionCheckoutResult> CreateCheckoutAsync(
        Guid userId,
        Guid planId,
        TorcedorSubscriptionPaymentMethod paymentMethod,
        CancellationToken cancellationToken = default)
    {
        if (paymentMethod == TorcedorSubscriptionPaymentMethod.Pix
            && string.Equals(paymentProvider.ProviderKey, "Stripe", StringComparison.OrdinalIgnoreCase))
        {
            return CreateTorcedorSubscriptionCheckoutResult.SubscribeFailed(
                SubscribeMemberError.GatewayDoesNotSupportPaymentMethod);
        }

        var subscribed = await mediator
            .Send(new SubscribeMemberCommand(userId, planId), cancellationToken)
            .ConfigureAwait(false);

        if (!subscribed.Ok)
            return CreateTorcedorSubscriptionCheckoutResult.SubscribeFailed(
                subscribed.Error ?? SubscribeMemberError.PlanNotFoundOrNotAvailable);

        var membershipId = subscribed.MembershipId!.Value;
        var plan = await db.MembershipPlans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken)
            .ConfigureAwait(false);
        if (plan is null)
            return CreateTorcedorSubscriptionCheckoutResult.SubscribeFailed(SubscribeMemberError.PlanNotFoundOrNotAvailable);

        var amount = CalculateChargeAmount(plan.Price, plan.DiscountPercentage);
        var paymentId = Guid.NewGuid();
        var utc = DateTimeOffset.UtcNow;

        TorcedorSubscriptionCheckoutPixDto? pix = null;
        TorcedorSubscriptionCheckoutCardDto? card = null;
        string externalReference = paymentId.ToString("N");

        if (paymentMethod == TorcedorSubscriptionPaymentMethod.Pix)
        {
            var r = await paymentProvider.CreatePixAsync(paymentId, amount, Currency, cancellationToken).ConfigureAwait(false);
            pix = new TorcedorSubscriptionCheckoutPixDto(r.QrCodePayload, r.CopyPasteKey);
        }
        else
        {
            var r = await paymentProvider.CreateCardAsync(paymentId, amount, Currency, cancellationToken).ConfigureAwait(false);
            card = new TorcedorSubscriptionCheckoutCardDto(r.CheckoutUrl);
            externalReference = r.ProviderReference ?? paymentId.ToString("N");
        }

        db.Payments.Add(
            new PaymentRecord
            {
                Id = paymentId,
                UserId = userId,
                MembershipId = membershipId,
                Amount = amount,
                Status = PaymentChargeStatuses.Pending,
                DueDate = utc.AddDays(1),
                PaidAt = null,
                PaymentMethod = paymentMethod == TorcedorSubscriptionPaymentMethod.Pix ? "Pix" : "Card",
                ExternalReference = externalReference,
                ProviderName = paymentProvider.ProviderKey,
                CreatedAt = utc,
                UpdatedAt = utc,
                StatusReason = "Cobrança gerada na contratação (D.4).",
            });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return CreateTorcedorSubscriptionCheckoutResult.Success(
            membershipId,
            paymentId,
            amount,
            Currency,
            paymentMethod,
            MembershipStatus.PendingPayment,
            pix,
            card);
    }

    public Task<ConfirmTorcedorSubscriptionPaymentResult> ConfirmPaymentAsync(
        Guid paymentId,
        string? webhookSecret,
        CancellationToken cancellationToken = default)
    {
        var expected = paymentsOptions.Value.WebhookSecret ?? string.Empty;
        if (string.IsNullOrWhiteSpace(expected) || !string.Equals(expected, webhookSecret, StringComparison.Ordinal))
            return Task.FromResult(ConfirmTorcedorSubscriptionPaymentResult.Failure(ConfirmTorcedorSubscriptionPaymentError.InvalidWebhookSecret));

        return ConfirmPaymentAfterProviderSuccessAsync(paymentId, providerPaymentReference: null, cancellationToken);
    }

    public async Task<ConfirmTorcedorSubscriptionPaymentResult> ConfirmPaymentAfterProviderSuccessAsync(
        Guid paymentId,
        string? providerPaymentReference,
        CancellationToken cancellationToken = default)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken).ConfigureAwait(false);
        if (payment is null)
            return ConfirmTorcedorSubscriptionPaymentResult.Failure(ConfirmTorcedorSubscriptionPaymentError.NotFound);

        if (payment.Status == PaymentChargeStatuses.Paid)
            return ConfirmTorcedorSubscriptionPaymentResult.Success();

        if (payment.Status is not (PaymentChargeStatuses.Pending or PaymentChargeStatuses.Overdue))
            return ConfirmTorcedorSubscriptionPaymentResult.Failure(ConfirmTorcedorSubscriptionPaymentError.InvalidState);

        var membership = await db.Memberships.FirstOrDefaultAsync(m => m.Id == payment.MembershipId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null)
            return ConfirmTorcedorSubscriptionPaymentResult.Failure(ConfirmTorcedorSubscriptionPaymentError.NotFound);

        var now = DateTimeOffset.UtcNow;
        var isPlanChangeProration =
            payment.StatusReason?.StartsWith(TorcedorPlanChangePaymentReasons.ProrationPrefix, StringComparison.Ordinal)
            == true;

        if (membership.Status == MembershipStatus.Ativo && isPlanChangeProration)
        {
            payment.Status = PaymentChargeStatuses.Paid;
            payment.PaidAt = now;
            payment.UpdatedAt = now;
            payment.LastProviderSyncAt = now;
            payment.StatusReason = "Pagamento confirmado — troca de plano (D.6).";
            if (!string.IsNullOrWhiteSpace(providerPaymentReference))
                payment.ExternalReference = providerPaymentReference;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await loyaltyPoints.AwardPointsForPaymentPaidAsync(payment.Id, cancellationToken).ConfigureAwait(false);

            return ConfirmTorcedorSubscriptionPaymentResult.Success();
        }

        if (membership.Status != MembershipStatus.PendingPayment)
            return ConfirmTorcedorSubscriptionPaymentResult.Failure(ConfirmTorcedorSubscriptionPaymentError.InvalidState);

        var plan = membership.PlanId is { } pid
            ? await db.MembershipPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pid, cancellationToken).ConfigureAwait(false)
            : null;

        payment.Status = PaymentChargeStatuses.Paid;
        payment.PaidAt = now;
        payment.UpdatedAt = now;
        payment.LastProviderSyncAt = now;
        payment.StatusReason = "Pagamento confirmado via webhook/callback (D.4).";
        if (!string.IsNullOrWhiteSpace(providerPaymentReference))
            payment.ExternalReference = providerPaymentReference;

        var fromStatus = membership.Status;
        membership.Status = MembershipStatus.Ativo;
        membership.NextDueDate = plan is null ? now.AddMonths(1) : AddBillingCycle(now, plan.BillingCycle);

        db.MembershipHistories.Add(
            new MembershipHistoryRecord
            {
                Id = Guid.NewGuid(),
                MembershipId = membership.Id,
                UserId = membership.UserId,
                EventType = MembershipHistoryEventTypes.StatusChanged,
                FromStatus = fromStatus,
                ToStatus = MembershipStatus.Ativo,
                FromPlanId = membership.PlanId,
                ToPlanId = membership.PlanId,
                Reason = "Pagamento confirmado — assinatura ativada.",
                ActorUserId = null,
                CreatedAt = now,
            });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await loyaltyPoints.AwardPointsForPaymentPaidAsync(payment.Id, cancellationToken).ConfigureAwait(false);

        return ConfirmTorcedorSubscriptionPaymentResult.Success();
    }

    private static decimal CalculateChargeAmount(decimal price, decimal discountPercentage)
    {
        if (discountPercentage <= 0)
            return Math.Round(price, 2, MidpointRounding.AwayFromZero);
        var p = price * (1 - discountPercentage / 100m);
        return Math.Round(p, 2, MidpointRounding.AwayFromZero);
    }

    private static DateTimeOffset AddBillingCycle(DateTimeOffset fromUtc, string billingCycle) =>
        billingCycle.Trim() switch
        {
            "Yearly" => fromUtc.AddYears(1),
            "Quarterly" => fromUtc.AddMonths(3),
            _ => fromUtc.AddMonths(1),
        };
}
