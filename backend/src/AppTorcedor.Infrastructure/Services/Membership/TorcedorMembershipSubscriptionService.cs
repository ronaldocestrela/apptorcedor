using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Membership;

public sealed class TorcedorMembershipSubscriptionService(AppDbContext db) : ITorcedorMembershipSubscriptionPort
{
    private const string SubscribeReason = "Contratação iniciada — aguardando confirmação de pagamento.";

    public async Task<SubscribeMemberResult> SubscribeToPlanAsync(
        Guid userId,
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        var planOk = await db.MembershipPlans.AsNoTracking()
            .AnyAsync(p => p.Id == planId && p.IsPublished && p.IsActive, cancellationToken)
            .ConfigureAwait(false);
        if (!planOk)
            return SubscribeMemberResult.Failure(SubscribeMemberError.PlanNotFoundOrNotAvailable);

        var membership = await db.Memberships
            .FirstOrDefaultAsync(m => m.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (membership is { Status: MembershipStatus.Ativo })
            return SubscribeMemberResult.Failure(SubscribeMemberError.AlreadyActiveSubscription);

        if (membership is { Status: MembershipStatus.PendingPayment })
            return SubscribeMemberResult.Failure(SubscribeMemberError.SubscriptionPendingPayment);

        if (membership is { Status: MembershipStatus.Inadimplente or MembershipStatus.Suspenso })
            return SubscribeMemberResult.Failure(SubscribeMemberError.MembershipStatusPreventsSubscribe);

        var utc = DateTimeOffset.UtcNow;
        Guid membershipId;
        MembershipStatus? fromStatus;
        Guid? fromPlanId;

        if (membership is null)
        {
            membershipId = Guid.NewGuid();
            fromStatus = null;
            fromPlanId = null;
            db.Memberships.Add(
                new MembershipRecord
                {
                    Id = membershipId,
                    UserId = userId,
                    PlanId = planId,
                    Status = MembershipStatus.PendingPayment,
                    StartDate = utc,
                    EndDate = null,
                    NextDueDate = null,
                });
        }
        else
        {
            membershipId = membership.Id;
            fromStatus = membership.Status;
            fromPlanId = membership.PlanId;
            membership.PlanId = planId;
            membership.Status = MembershipStatus.PendingPayment;
            membership.StartDate = utc;
            membership.EndDate = null;
            membership.NextDueDate = null;
        }

        db.MembershipHistories.Add(
            new MembershipHistoryRecord
            {
                Id = Guid.NewGuid(),
                MembershipId = membershipId,
                UserId = userId,
                EventType = MembershipHistoryEventTypes.Subscribed,
                FromStatus = fromStatus,
                ToStatus = MembershipStatus.PendingPayment,
                FromPlanId = fromPlanId,
                ToPlanId = planId,
                Reason = SubscribeReason,
                ActorUserId = userId,
                CreatedAt = utc,
            });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return SubscribeMemberResult.Success(membershipId, userId, planId, MembershipStatus.PendingPayment);
    }
}
