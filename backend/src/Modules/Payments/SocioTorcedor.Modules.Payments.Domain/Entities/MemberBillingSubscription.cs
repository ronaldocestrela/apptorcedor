using SocioTorcedor.BuildingBlocks.Domain.Abstractions;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Domain.Entities;

/// <summary>
/// Assinatura do sócio no banco do tenant (clube cobra o sócio).
/// </summary>
public sealed class MemberBillingSubscription : AggregateRoot
{
    private MemberBillingSubscription()
    {
    }

    public Guid MemberProfileId { get; private set; }

    public Guid MemberPlanId { get; private set; }

    public decimal RecurringAmount { get; private set; }

    public string Currency { get; private set; } = "BRL";

    public PaymentMethodKind PaymentMethod { get; private set; }

    public BillingSubscriptionStatus Status { get; private set; }

    public string? ExternalCustomerId { get; private set; }

    public string? ExternalSubscriptionId { get; private set; }

    public DateTime? NextBillingAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public ICollection<MemberBillingInvoice> Invoices { get; } = new List<MemberBillingInvoice>();

    public static MemberBillingSubscription Start(
        Guid memberProfileId,
        Guid memberPlanId,
        decimal recurringAmount,
        string currency,
        PaymentMethodKind paymentMethod,
        string? externalCustomerId,
        string? externalSubscriptionId,
        BillingSubscriptionStatus initialStatus,
        DateTime? nextBillingAtUtc)
    {
        if (recurringAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(recurringAmount));

        var now = DateTime.UtcNow;
        return new MemberBillingSubscription
        {
            MemberProfileId = memberProfileId,
            MemberPlanId = memberPlanId,
            RecurringAmount = recurringAmount,
            Currency = string.IsNullOrWhiteSpace(currency) ? "BRL" : currency.Trim().ToUpperInvariant(),
            PaymentMethod = paymentMethod,
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
}
