using SocioTorcedor.Modules.Payments.Domain.Entities;

namespace SocioTorcedor.Modules.Payments.Application.Contracts;

public interface ITenantMasterPaymentsRepository
{
    Task<TenantBillingSubscription?> GetActiveSubscriptionByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task AddSubscriptionAsync(TenantBillingSubscription subscription, CancellationToken cancellationToken);

    Task AddInvoiceAsync(TenantBillingInvoice invoice, CancellationToken cancellationToken);

    Task<TenantPaymentWebhookInbox?> GetWebhookByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);

    Task AddWebhookAsync(TenantPaymentWebhookInbox inbox, CancellationToken cancellationToken);

    Task<IReadOnlyList<TenantBillingInvoice>> ListInvoicesByTenantAsync(
        Guid tenantId,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<int> CountInvoicesByTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<TenantBillingSubscription?> GetSubscriptionByExternalIdAsync(string externalSubscriptionId, CancellationToken cancellationToken);

    Task<TenantMemberGatewayConfiguration?> GetMemberGatewayConfigurationByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    Task AddMemberGatewayConfigurationAsync(TenantMemberGatewayConfiguration configuration, CancellationToken cancellationToken);

    Task<MemberStripeWebhookInbox?> GetMemberStripeWebhookByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task AddMemberStripeWebhookAsync(MemberStripeWebhookInbox inbox, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
