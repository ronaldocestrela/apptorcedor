using Microsoft.EntityFrameworkCore;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Domain.Entities;
using SocioTorcedor.Modules.Payments.Domain.Enums;
using SocioTorcedor.Modules.Payments.Infrastructure.Persistence;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Repositories;

public sealed class MemberTenantPaymentsRepository(TenantPaymentsDbContext db) : IMemberTenantPaymentsRepository
{
    public Task AddInvoiceAsync(MemberBillingInvoice invoice, CancellationToken cancellationToken) =>
        db.MemberBillingInvoices.AddAsync(invoice, cancellationToken).AsTask();

    public Task AddSubscriptionAsync(MemberBillingSubscription subscription, CancellationToken cancellationToken) =>
        db.MemberBillingSubscriptions.AddAsync(subscription, cancellationToken).AsTask();

    public Task AddWebhookAsync(MemberPaymentWebhookInbox inbox, CancellationToken cancellationToken) =>
        db.MemberPaymentWebhookInbox.AddAsync(inbox, cancellationToken).AsTask();

    public Task<int> CountInvoicesByMemberAsync(Guid memberProfileId, CancellationToken cancellationToken) =>
        db.MemberBillingInvoices
            .Where(i => db.MemberBillingSubscriptions.Any(s => s.Id == i.MemberBillingSubscriptionId && s.MemberProfileId == memberProfileId))
            .CountAsync(cancellationToken);

    public Task<MemberBillingSubscription?> GetActiveSubscriptionByMemberAsync(
        Guid memberProfileId,
        CancellationToken cancellationToken) =>
        db.MemberBillingSubscriptions
            .Include(x => x.Invoices)
            .Where(x =>
                x.MemberProfileId == memberProfileId &&
                x.Status != BillingSubscriptionStatus.Canceled)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<MemberPaymentWebhookInbox?> GetWebhookByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        db.MemberPaymentWebhookInbox.FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

    public Task<MemberBillingSubscription?> GetSubscriptionByExternalIdAsync(
        string externalSubscriptionId,
        CancellationToken cancellationToken) =>
        db.MemberBillingSubscriptions.FirstOrDefaultAsync(
            x => x.ExternalSubscriptionId == externalSubscriptionId,
            cancellationToken);

    public async Task<IReadOnlyList<MemberBillingInvoice>> ListInvoicesByMemberAsync(
        Guid memberProfileId,
        int skip,
        int take,
        CancellationToken cancellationToken) =>
        await db.MemberBillingInvoices
            .Include(i => i.Subscription)
            .Where(i => db.MemberBillingSubscriptions.Any(s => s.Id == i.MemberBillingSubscriptionId && s.MemberProfileId == memberProfileId))
            .OrderByDescending(i => i.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
