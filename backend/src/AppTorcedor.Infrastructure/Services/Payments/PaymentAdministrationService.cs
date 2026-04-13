using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Payments;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Services.Payments;

public sealed class PaymentAdministrationService(
    AppDbContext db,
    IPaymentProvider paymentProvider,
    IMembershipAdministrationPort membershipAdmin) : IPaymentsAdministrationPort
{
    public async Task<AdminPaymentListPageDto> ListPaymentsAsync(
        string? status,
        Guid? userId,
        Guid? membershipId,
        string? paymentMethod,
        DateTimeOffset? dueFrom,
        DateTimeOffset? dueTo,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query =
            from p in db.Payments.AsNoTracking()
            join u in db.Users.AsNoTracking() on p.UserId equals u.Id
            select new { p, u };

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.p.Status == status);
        if (userId is { } uid)
            query = query.Where(x => x.p.UserId == uid);
        if (membershipId is { } mid)
            query = query.Where(x => x.p.MembershipId == mid);
        if (!string.IsNullOrWhiteSpace(paymentMethod))
            query = query.Where(x => x.p.PaymentMethod == paymentMethod);
        if (dueFrom is { } df)
            query = query.Where(x => x.p.DueDate >= df);
        if (dueTo is { } dt)
            query = query.Where(x => x.p.DueDate <= dt);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await query
            .OrderByDescending(x => x.p.DueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = rows.Select(x => new AdminPaymentListItemDto(
                x.p.Id,
                x.p.UserId,
                x.u.Email ?? string.Empty,
                x.u.Name,
                x.p.MembershipId,
                x.p.Amount,
                x.p.Status,
                x.p.DueDate,
                x.p.PaidAt,
                x.p.PaymentMethod,
                x.p.ExternalReference))
            .ToList();

        return new AdminPaymentListPageDto(total, items);
    }

    public async Task<AdminPaymentDetailDto?> GetPaymentByIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var row = await (
                from p in db.Payments.AsNoTracking()
                join u in db.Users.AsNoTracking() on p.UserId equals u.Id
                where p.Id == paymentId
                select new { p, u })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (row is null)
            return null;

        return new AdminPaymentDetailDto(
            row.p.Id,
            row.p.UserId,
            row.u.Email ?? string.Empty,
            row.u.Name,
            row.p.MembershipId,
            row.p.Amount,
            row.p.Status,
            row.p.DueDate,
            row.p.PaidAt,
            row.p.PaymentMethod,
            row.p.ExternalReference,
            row.p.ProviderName,
            row.p.CancelledAt,
            row.p.RefundedAt,
            row.p.CreatedAt,
            row.p.UpdatedAt,
            row.p.LastProviderSyncAt,
            row.p.StatusReason);
    }

    public async Task<PaymentMutationResult> ConciliatePaymentAsync(
        Guid paymentId,
        DateTimeOffset? paidAt,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken).ConfigureAwait(false);
        if (payment is null)
            return new PaymentMutationResult(false, PaymentMutationError.NotFound);

        if (payment.Status is PaymentChargeStatuses.Paid)
            return new PaymentMutationResult(false, PaymentMutationError.InvalidTransition);

        if (payment.Status is not (PaymentChargeStatuses.Pending or PaymentChargeStatuses.Overdue))
            return new PaymentMutationResult(false, PaymentMutationError.InvalidTransition);

        var now = DateTimeOffset.UtcNow;
        payment.Status = PaymentChargeStatuses.Paid;
        payment.PaidAt = paidAt ?? now;
        payment.UpdatedAt = now;
        payment.LastProviderSyncAt = now;
        payment.StatusReason = "Conciliação manual (admin).";

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await TryReactivateMembershipAfterPaidAsync(payment.MembershipId, cancellationToken).ConfigureAwait(false);

        return new PaymentMutationResult(true, null);
    }

    public async Task<PaymentMutationResult> CancelPaymentAsync(
        Guid paymentId,
        string? reason,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken).ConfigureAwait(false);
        if (payment is null)
            return new PaymentMutationResult(false, PaymentMutationError.NotFound);

        if (payment.Status is not (PaymentChargeStatuses.Pending or PaymentChargeStatuses.Overdue))
            return new PaymentMutationResult(false, PaymentMutationError.InvalidTransition);

        await paymentProvider.CancelAsync(payment.Id, payment.ExternalReference, cancellationToken).ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;
        payment.Status = PaymentChargeStatuses.Cancelled;
        payment.CancelledAt = now;
        payment.UpdatedAt = now;
        payment.LastProviderSyncAt = now;
        payment.StatusReason = string.IsNullOrWhiteSpace(reason) ? "Cancelamento manual (admin)." : reason.Trim();

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new PaymentMutationResult(true, null);
    }

    public async Task<PaymentMutationResult> RefundPaymentAsync(
        Guid paymentId,
        string? reason,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken).ConfigureAwait(false);
        if (payment is null)
            return new PaymentMutationResult(false, PaymentMutationError.NotFound);

        if (payment.Status != PaymentChargeStatuses.Paid)
            return new PaymentMutationResult(false, PaymentMutationError.InvalidTransition);

        await paymentProvider.RefundAsync(payment.Id, payment.ExternalReference, cancellationToken).ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;
        payment.Status = PaymentChargeStatuses.Refunded;
        payment.RefundedAt = now;
        payment.UpdatedAt = now;
        payment.LastProviderSyncAt = now;
        payment.StatusReason = string.IsNullOrWhiteSpace(reason) ? "Estorno manual (admin)." : reason.Trim();

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new PaymentMutationResult(true, null);
    }

    private async Task TryReactivateMembershipAfterPaidAsync(Guid membershipId, CancellationToken cancellationToken)
    {
        var membership = await db.Memberships.FirstOrDefaultAsync(m => m.Id == membershipId, cancellationToken).ConfigureAwait(false);
        if (membership is null || membership.Status != MembershipStatus.Inadimplente)
            return;

        var hasOpen = await db.Payments.AnyAsync(
                p => p.MembershipId == membershipId
                    && (p.Status == PaymentChargeStatuses.Pending || p.Status == PaymentChargeStatuses.Overdue),
                cancellationToken)
            .ConfigureAwait(false);
        if (hasOpen)
            return;

        await membershipAdmin
            .ApplySystemMembershipTransitionAsync(
                membershipId,
                MembershipStatus.Ativo,
                "Pagamento conciliado — sem cobranças pendentes ou vencidas em aberto.",
                cancellationToken)
            .ConfigureAwait(false);
    }
}
