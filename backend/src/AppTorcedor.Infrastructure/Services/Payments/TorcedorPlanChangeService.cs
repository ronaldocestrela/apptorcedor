using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Payments;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Payments;

public sealed class TorcedorPlanChangeService(
    AppDbContext db,
    IPaymentProvider paymentProvider,
    TimeProvider timeProvider) : ITorcedorPlanChangePort
{
    private const string Currency = "BRL";
    private const string ProviderName = "Mock";

    public async Task<ChangePlanResult> ChangePlanAsync(
        Guid userId,
        Guid newPlanId,
        TorcedorSubscriptionPaymentMethod paymentMethod,
        CancellationToken cancellationToken = default)
    {
        var membership = await db.Memberships
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.StartDate)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (membership is null)
            return ChangePlanResult.Failure(ChangePlanError.MembershipNotFound);

        if (membership.Status != MembershipStatus.Ativo)
            return ChangePlanResult.Failure(ChangePlanError.MembershipNotActive);

        if (membership.PlanId is not { } currentPlanId)
            return ChangePlanResult.Failure(ChangePlanError.MissingBillingCycleContext);

        if (membership.NextDueDate is null)
            return ChangePlanResult.Failure(ChangePlanError.MissingBillingCycleContext);

        if (newPlanId == currentPlanId)
            return ChangePlanResult.Failure(ChangePlanError.SamePlan);

        var currentPlan = await db.MembershipPlans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == currentPlanId, cancellationToken)
            .ConfigureAwait(false);
        if (currentPlan is null)
            return ChangePlanResult.Failure(ChangePlanError.MissingBillingCycleContext);

        var newPlan = await db.MembershipPlans.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == newPlanId && p.IsPublished && p.IsActive,
                cancellationToken)
            .ConfigureAwait(false);
        if (newPlan is null)
            return ChangePlanResult.Failure(ChangePlanError.PlanNotFoundOrNotAvailable);

        var now = timeProvider.GetUtcNow();
        var cycleEnd = membership.NextDueDate.Value;
        var cycleStart = SubtractBillingCycle(cycleEnd, currentPlan.BillingCycle);
        var totalTicks = cycleEnd - cycleStart;
        var totalDays = Math.Max(1e-6, totalTicks.TotalDays);
        var remainingTicks = cycleEnd - now;
        var remainingDays = Math.Clamp(remainingTicks.TotalDays, 0, totalDays);
        var fraction = (decimal)(remainingDays / totalDays);

        var oldEffective = CalculateChargeAmount(currentPlan.Price, currentPlan.DiscountPercentage);
        var newEffective = CalculateChargeAmount(newPlan.Price, newPlan.DiscountPercentage);
        var rawDelta = (newEffective - oldEffective) * fraction;
        var proration = Math.Max(0m, Math.Round(rawDelta, 2, MidpointRounding.AwayFromZero));

        var openPayments = await db.Payments
            .Where(
                p => p.MembershipId == membership.Id
                    && (p.Status == PaymentChargeStatuses.Pending || p.Status == PaymentChargeStatuses.Overdue))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var p in openPayments)
        {
            await paymentProvider.CancelAsync(p.Id, p.ExternalReference, cancellationToken).ConfigureAwait(false);
            p.Status = PaymentChargeStatuses.Cancelled;
            p.CancelledAt = now;
            p.UpdatedAt = now;
            p.LastProviderSyncAt = now;
            p.StatusReason = string.IsNullOrWhiteSpace(p.StatusReason)
                ? "Cancelada na troca de plano (D.6)."
                : $"{p.StatusReason} — cancelada na troca de plano (D.6).";
        }

        var fromSnapshot = new ChangePlanPlanSnapshotDto(
            currentPlan.Id,
            currentPlan.Name,
            currentPlan.Price,
            currentPlan.BillingCycle,
            currentPlan.DiscountPercentage);
        var toSnapshot = new ChangePlanPlanSnapshotDto(
            newPlan.Id,
            newPlan.Name,
            newPlan.Price,
            newPlan.BillingCycle,
            newPlan.DiscountPercentage);

        membership.PlanId = newPlanId;

        db.MembershipHistories.Add(
            new MembershipHistoryRecord
            {
                Id = Guid.NewGuid(),
                MembershipId = membership.Id,
                UserId = membership.UserId,
                EventType = MembershipHistoryEventTypes.PlanChanged,
                FromStatus = membership.Status,
                ToStatus = membership.Status,
                FromPlanId = currentPlanId,
                ToPlanId = newPlanId,
                Reason = $"Troca de plano — proporcional {proration:F2} {Currency} (fração {fraction:P2}).",
                ActorUserId = userId,
                CreatedAt = now,
            });

        TorcedorSubscriptionCheckoutPixDto? pix = null;
        TorcedorSubscriptionCheckoutCardDto? card = null;
        Guid? paymentId = null;

        if (proration > 0)
        {
            paymentId = Guid.NewGuid();
            if (paymentMethod == TorcedorSubscriptionPaymentMethod.Pix)
            {
                var r = await paymentProvider
                    .CreatePixAsync(paymentId.Value, proration, Currency, cancellationToken)
                    .ConfigureAwait(false);
                pix = new TorcedorSubscriptionCheckoutPixDto(r.QrCodePayload, r.CopyPasteKey);
            }
            else
            {
                var r = await paymentProvider
                    .CreateCardAsync(paymentId.Value, proration, Currency, cancellationToken)
                    .ConfigureAwait(false);
                card = new TorcedorSubscriptionCheckoutCardDto(r.CheckoutUrl);
            }

            db.Payments.Add(
                new PaymentRecord
                {
                    Id = paymentId.Value,
                    UserId = userId,
                    MembershipId = membership.Id,
                    Amount = proration,
                    Status = PaymentChargeStatuses.Pending,
                    DueDate = now.AddDays(1),
                    PaidAt = null,
                    PaymentMethod = paymentMethod == TorcedorSubscriptionPaymentMethod.Pix ? "Pix" : "Card",
                    ExternalReference = paymentId.Value.ToString("N"),
                    ProviderName = ProviderName,
                    CreatedAt = now,
                    UpdatedAt = now,
                    StatusReason = TorcedorPlanChangePaymentReasons.ProrationPrefix,
                });
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ChangePlanResult.Success(
            membership.Id,
            membership.Status,
            fromSnapshot,
            toSnapshot,
            proration,
            paymentId,
            Currency,
            proration > 0 ? paymentMethod : null,
            pix,
            card);
    }

    private static decimal CalculateChargeAmount(decimal price, decimal discountPercentage)
    {
        if (discountPercentage <= 0)
            return Math.Round(price, 2, MidpointRounding.AwayFromZero);
        var p = price * (1 - discountPercentage / 100m);
        return Math.Round(p, 2, MidpointRounding.AwayFromZero);
    }

    private static DateTimeOffset SubtractBillingCycle(DateTimeOffset endUtc, string billingCycle) =>
        billingCycle.Trim() switch
        {
            "Yearly" => endUtc.AddYears(-1),
            "Quarterly" => endUtc.AddMonths(-3),
            _ => endUtc.AddMonths(-1),
        };
}
