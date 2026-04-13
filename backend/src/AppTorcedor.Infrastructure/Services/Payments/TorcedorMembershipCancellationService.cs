using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Payments;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Payments;

public sealed class TorcedorMembershipCancellationService(
    AppDbContext db,
    IPaymentProvider paymentProvider,
    TimeProvider timeProvider,
    IAppConfigurationPort appConfiguration) : ITorcedorMembershipCancellationPort
{
    public async Task<CancelMembershipResult> CancelMembershipAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var membership = await db.Memberships
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.StartDate)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (membership is null)
            return CancelMembershipResult.Failure(CancelMembershipError.MembershipNotFound);

        var now = timeProvider.GetUtcNow();

        if (membership.Status == MembershipStatus.Ativo
            && membership.EndDate is { } effectiveEnd
            && effectiveEnd <= now)
        {
            ApplyEffectiveCancellationForMembership(membership, now);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return CancelMembershipResult.Failure(CancelMembershipError.MembershipAlreadyCancelled);
        }

        if (membership.Status == MembershipStatus.Cancelado)
            return CancelMembershipResult.Failure(CancelMembershipError.MembershipAlreadyCancelled);

        if (membership.Status == MembershipStatus.Ativo
            && membership.EndDate is { } pendingEnd
            && pendingEnd > now)
            return CancelMembershipResult.Failure(CancelMembershipError.CancellationAlreadyScheduled);

        if (membership.Status
            is not (MembershipStatus.Ativo or MembershipStatus.PendingPayment))
            return CancelMembershipResult.Failure(CancelMembershipError.MembershipNotCancellable);

        var coolingOffDays = await GetCoolingOffDaysAsync(cancellationToken).ConfigureAwait(false);
        var daysSinceStart = (now - membership.StartDate).TotalDays;
        var withinCoolingOff = daysSinceStart <= coolingOffDays;

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
                ? "Cancelada no cancelamento de assinatura pelo torcedor (D.7)."
                : $"{p.StatusReason} — cancelada no cancelamento pelo torcedor (D.7).";
        }

        if (membership.Status == MembershipStatus.PendingPayment || withinCoolingOff)
        {
            var wasPending = membership.Status == MembershipStatus.PendingPayment;
            var fromStatus = membership.Status;
            membership.Status = MembershipStatus.Cancelado;
            membership.NextDueDate = null;
            membership.EndDate = now;

            db.MembershipHistories.Add(
                new MembershipHistoryRecord
                {
                    Id = Guid.NewGuid(),
                    MembershipId = membership.Id,
                    UserId = membership.UserId,
                    EventType = MembershipHistoryEventTypes.CancelledByMember,
                    FromStatus = fromStatus,
                    ToStatus = MembershipStatus.Cancelado,
                    FromPlanId = membership.PlanId,
                    ToPlanId = membership.PlanId,
                    Reason = wasPending
                        ? "Cancelamento pelo torcedor — contratação ainda não paga."
                        : $"Cancelamento imediato pelo torcedor dentro do prazo de arrependimento ({coolingOffDays} dias).",
                    ActorUserId = userId,
                    CreatedAt = now,
                });

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return CancelMembershipResult.Success(
                membership.Id,
                MembershipStatus.Cancelado,
                TorcedorMembershipCancellationMode.Immediate,
                now,
                "Sua assinatura foi cancelada imediatamente.");
        }

        if (membership.NextDueDate is null)
            return CancelMembershipResult.Failure(CancelMembershipError.MissingBillingContext);

        var accessUntil = membership.NextDueDate.Value;
        membership.EndDate = accessUntil;

        db.MembershipHistories.Add(
            new MembershipHistoryRecord
            {
                Id = Guid.NewGuid(),
                MembershipId = membership.Id,
                UserId = membership.UserId,
                EventType = MembershipHistoryEventTypes.CancelledByMember,
                FromStatus = membership.Status,
                ToStatus = membership.Status,
                FromPlanId = membership.PlanId,
                ToPlanId = membership.PlanId,
                Reason =
                    $"Cancelamento agendado pelo torcedor — acesso mantido até {accessUntil:O} (fora do prazo de arrependimento de {coolingOffDays} dias).",
                ActorUserId = userId,
                CreatedAt = now,
            });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return CancelMembershipResult.Success(
            membership.Id,
            MembershipStatus.Ativo,
            TorcedorMembershipCancellationMode.ScheduledEndOfCycle,
            accessUntil,
            $"Seu acesso permanece ativo até {accessUntil:dd/MM/yyyy HH:mm} (UTC).");
    }

    private async Task<int> GetCoolingOffDaysAsync(CancellationToken cancellationToken)
    {
        var entry = await appConfiguration
            .GetAsync(TorcedorMembershipCancellationConfigKeys.CoolingOffDays, cancellationToken)
            .ConfigureAwait(false);
        if (entry is null || !int.TryParse(entry.Value.Trim(), out var days) || days < 0)
            return TorcedorMembershipCancellationDefaults.DefaultCoolingOffDays;
        return Math.Min(days, 365);
    }

    private void ApplyEffectiveCancellationForMembership(MembershipRecord m, DateTimeOffset nowUtc)
    {
        var from = m.Status;
        m.Status = MembershipStatus.Cancelado;
        m.NextDueDate = null;

        db.MembershipHistories.Add(
            new MembershipHistoryRecord
            {
                Id = Guid.NewGuid(),
                MembershipId = m.Id,
                UserId = m.UserId,
                EventType = MembershipHistoryEventTypes.StatusChanged,
                FromStatus = from,
                ToStatus = MembershipStatus.Cancelado,
                FromPlanId = m.PlanId,
                ToPlanId = m.PlanId,
                Reason = "Encerramento efetivo após cancelamento agendado pelo torcedor (D.7).",
                ActorUserId = null,
                CreatedAt = nowUtc,
            });
    }
}
