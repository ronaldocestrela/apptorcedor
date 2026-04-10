using SocioTorcedor.Modules.Payments.Domain.Entities;

namespace SocioTorcedor.Modules.Payments.Application.Contracts;

public interface IMemberTenantPaymentsRepository
{
    Task<MemberBillingSubscription?> GetActiveSubscriptionByMemberAsync(Guid memberProfileId, CancellationToken cancellationToken);

    Task AddSubscriptionAsync(MemberBillingSubscription subscription, CancellationToken cancellationToken);

    Task AddInvoiceAsync(MemberBillingInvoice invoice, CancellationToken cancellationToken);

    Task<MemberPaymentWebhookInbox?> GetWebhookByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);

    Task AddWebhookAsync(MemberPaymentWebhookInbox inbox, CancellationToken cancellationToken);

    Task<MemberBillingSubscription?> GetSubscriptionByExternalIdAsync(string externalSubscriptionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MemberBillingInvoice>> ListInvoicesByMemberAsync(
        Guid memberProfileId,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<int> CountInvoicesByMemberAsync(Guid memberProfileId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
