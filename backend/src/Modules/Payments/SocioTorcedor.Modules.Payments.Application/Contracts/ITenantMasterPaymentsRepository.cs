using SocioTorcedor.Modules.Payments.Domain.Entities;

namespace SocioTorcedor.Modules.Payments.Application.Contracts;

public interface ITenantMasterPaymentsRepository
{
    Task<TenantBillingSubscription?> GetActiveSubscriptionByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task AddSubscriptionAsync(TenantBillingSubscription subscription, CancellationToken cancellationToken);

    Task AddInvoiceAsync(TenantBillingInvoice invoice, CancellationToken cancellationToken);

    Task<TenantPaymentWebhookInbox?> GetWebhookByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);

    Task AddWebhookAsync(TenantPaymentWebhookInbox inbox, CancellationToken cancellationToken);

    Task<ConnectStripeWebhookInbox?> GetConnectWebhookByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);

    Task AddConnectWebhookAsync(ConnectStripeWebhookInbox inbox, CancellationToken cancellationToken);

    Task<IReadOnlyList<TenantBillingInvoice>> ListInvoicesByTenantAsync(
        Guid tenantId,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<int> CountInvoicesByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<TenantBillingSubscription?> GetSubscriptionByExternalIdAsync(string externalSubscriptionId, CancellationToken cancellationToken);

    Task<TenantStripeConnectAccount?> GetStripeConnectByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<TenantStripeConnectAccount?> GetStripeConnectByStripeAccountIdAsync(string stripeAccountId, CancellationToken cancellationToken);

    Task AddStripeConnectAsync(TenantStripeConnectAccount account, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
