using SocioTorcedor.BuildingBlocks.Domain.Abstractions;
using SocioTorcedor.Modules.Backoffice.Domain.Enums;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Domain.Entities;

/// <summary>
/// Assinatura de cobrança SaaS no banco master (clube paga o SaaS).
/// </summary>
public sealed class TenantBillingSubscription : AggregateRoot
{
    private TenantBillingSubscription()
    {
    }

    public Guid TenantId { get; private set; }

    public Guid TenantPlanId { get; private set; }

    public Guid SaaSPlanId { get; private set; }

    public BillingCycle BillingCycle { get; private set; }

    public decimal RecurringAmount { get; private set; }

    public string Currency { get; private set; } = "BRL";

    public BillingSubscriptionStatus Status { get; private set; }

    public string? ExternalCustomerId { get; private set; }

    public string? ExternalSubscriptionId { get; private set; }

    /// <summary>Price Stripe usado nesta assinatura (auditoria).</summary>
    public string? StripePriceId { get; private set; }

    /// <summary>Fim do período de cobrança atual na Stripe (UTC).</summary>
    public DateTime? CurrentPeriodEndUtc { get; private set; }

    public DateTime? NextBillingAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public ICollection<TenantBillingInvoice> Invoices { get; } = new List<TenantBillingInvoice>();

    public static TenantBillingSubscription Start(
        Guid tenantId,
        Guid tenantPlanId,
        Guid saasPlanId,
        BillingCycle billingCycle,
        decimal recurringAmount,
        string currency,
        string? externalCustomerId,
        string? externalSubscriptionId,
        BillingSubscriptionStatus initialStatus,
        DateTime? nextBillingAtUtc)
    {
        if (recurringAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(recurringAmount));

        var now = DateTime.UtcNow;
        return new TenantBillingSubscription
        {
            TenantId = tenantId,
            TenantPlanId = tenantPlanId,
            SaaSPlanId = saasPlanId,
            BillingCycle = billingCycle,
            RecurringAmount = recurringAmount,
            Currency = string.IsNullOrWhiteSpace(currency) ? "BRL" : currency.Trim().ToUpperInvariant(),
            Status = initialStatus,
            ExternalCustomerId = externalCustomerId,
            ExternalSubscriptionId = externalSubscriptionId,
            NextBillingAtUtc = nextBillingAtUtc,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void AttachProviderIds(string? externalCustomerId, string? externalSubscriptionId)
    {
        ExternalCustomerId = externalCustomerId;
        ExternalSubscriptionId = externalSubscriptionId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkStatus(BillingSubscriptionStatus status, DateTime? nextBillingAtUtc = null)
    {
        Status = status;
        if (nextBillingAtUtc.HasValue)
            NextBillingAtUtc = nextBillingAtUtc;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetStripeBillingMetadata(string? stripePriceId, DateTime? currentPeriodEndUtc)
    {
        StripePriceId = string.IsNullOrWhiteSpace(stripePriceId) ? null : stripePriceId.Trim();
        if (currentPeriodEndUtc.HasValue)
            CurrentPeriodEndUtc = currentPeriodEndUtc;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
