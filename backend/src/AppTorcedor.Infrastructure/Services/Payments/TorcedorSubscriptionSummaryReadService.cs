using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Payments;

public sealed class TorcedorSubscriptionSummaryReadService(AppDbContext db, IDigitalCardTorcedorPort digitalCardPort)
    : ITorcedorSubscriptionSummaryPort
{
    private const string Currency = "BRL";

    public async Task<MySubscriptionSummaryDto> GetMySubscriptionSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var membership = await db.Memberships.AsNoTracking()
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.StartDate)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (membership is null)
        {
            return new MySubscriptionSummaryDto(
                false,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        MySubscriptionSummaryPlanDto? planDto = null;
        if (membership.PlanId is { } planId)
        {
            var plan = await db.MembershipPlans.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken)
                .ConfigureAwait(false);
            if (plan is not null)
            {
                planDto = new MySubscriptionSummaryPlanDto(
                    plan.Id,
                    plan.Name,
                    plan.Price,
                    plan.BillingCycle,
                    plan.DiscountPercentage);
            }
        }

        var lastPayment = await db.Payments.AsNoTracking()
            .Where(p => p.MembershipId == membership.Id)
            .OrderByDescending(p => p.PaidAt ?? p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        MySubscriptionSummaryPaymentDto? paymentDto = null;
        if (lastPayment is not null)
        {
            paymentDto = new MySubscriptionSummaryPaymentDto(
                lastPayment.Id,
                lastPayment.Amount,
                Currency,
                lastPayment.Status,
                lastPayment.PaymentMethod,
                lastPayment.PaidAt,
                lastPayment.DueDate);
        }

        var cardView = await digitalCardPort.GetMyDigitalCardAsync(userId, cancellationToken).ConfigureAwait(false);
        var digitalCard = new MySubscriptionSummaryDigitalCardDto(
            cardView.State,
            cardView.MembershipStatus,
            cardView.Message);

        return new MySubscriptionSummaryDto(
            true,
            membership.Id,
            membership.Status.ToString(),
            membership.StartDate,
            membership.EndDate,
            membership.NextDueDate,
            planDto,
            paymentDto,
            digitalCard);
    }
}
