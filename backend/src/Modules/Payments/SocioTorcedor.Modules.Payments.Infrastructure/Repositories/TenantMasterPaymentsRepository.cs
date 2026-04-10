using Microsoft.EntityFrameworkCore;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Domain.Entities;
using SocioTorcedor.Modules.Payments.Domain.Enums;
using SocioTorcedor.Modules.Payments.Infrastructure.Persistence;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Repositories;

public sealed class TenantMasterPaymentsRepository(PaymentsMasterDbContext db) : ITenantMasterPaymentsRepository
{
    public Task AddInvoiceAsync(TenantBillingInvoice invoice, CancellationToken cancellationToken) =>
        db.TenantBillingInvoices.AddAsync(invoice, cancellationToken).AsTask();

    public Task AddSubscriptionAsync(TenantBillingSubscription subscription, CancellationToken cancellationToken) =>
        db.TenantBillingSubscriptions.AddAsync(subscription, cancellationToken).AsTask();

    public Task AddWebhookAsync(TenantPaymentWebhookInbox inbox, CancellationToken cancellationToken) =>
        db.TenantPaymentWebhookInbox.AddAsync(inbox, cancellationToken).AsTask();

    public Task<int> CountInvoicesByTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        db.TenantBillingInvoices
            .Where(i => db.TenantBillingSubscriptions.Any(s => s.Id == i.TenantBillingSubscriptionId && s.TenantId == tenantId))
            .CountAsync(cancellationToken);

    public Task<TenantBillingSubscription?> GetActiveSubscriptionByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken) =>
        db.TenantBillingSubscriptions
            .Include(x => x.Invoices)
            .Where(x =>
                x.TenantId == tenantId &&
                x.Status != BillingSubscriptionStatus.Canceled)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<TenantPaymentWebhookInbox?> GetWebhookByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        db.TenantPaymentWebhookInbox.FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

    public Task<TenantBillingSubscription?> GetSubscriptionByExternalIdAsync(
        string externalSubscriptionId,
        CancellationToken cancellationToken) =>
        db.TenantBillingSubscriptions.FirstOrDefaultAsync(
            x => x.ExternalSubscriptionId == externalSubscriptionId,
            cancellationToken);

    public async Task<IReadOnlyList<TenantBillingInvoice>> ListInvoicesByTenantAsync(
        Guid tenantId,
        int skip,
        int take,
        CancellationToken cancellationToken) =>
        await db.TenantBillingInvoices
            .Include(i => i.Subscription)
            .Where(i => db.TenantBillingSubscriptions.Any(s => s.Id == i.TenantBillingSubscriptionId && s.TenantId == tenantId))
            .OrderByDescending(i => i.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
