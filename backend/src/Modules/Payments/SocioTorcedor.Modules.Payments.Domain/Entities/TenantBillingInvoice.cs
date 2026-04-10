using SocioTorcedor.BuildingBlocks.Domain.Abstractions;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Domain.Entities;

public sealed class TenantBillingInvoice : AggregateRoot
{
    private TenantBillingInvoice()
    {
    }

    public Guid TenantBillingSubscriptionId { get; private set; }

    public TenantBillingSubscription? Subscription { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = "BRL";

    public DateTime DueAtUtc { get; private set; }

    public BillingInvoiceStatus Status { get; private set; }

    public string? ExternalInvoiceId { get; private set; }

    public DateTime? PaidAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static TenantBillingInvoice Create(
        Guid subscriptionId,
        decimal amount,
        string currency,
        DateTime dueAtUtc,
        BillingInvoiceStatus status,
        string? externalInvoiceId)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        var now = DateTime.UtcNow;
        return new TenantBillingInvoice
        {
            TenantBillingSubscriptionId = subscriptionId,
            Amount = amount,
            Currency = string.IsNullOrWhiteSpace(currency) ? "BRL" : currency.Trim().ToUpperInvariant(),
            DueAtUtc = dueAtUtc,
            Status = status,
            ExternalInvoiceId = externalInvoiceId,
            CreatedAtUtc = now
        };
    }

    public void MarkPaid(DateTime paidAtUtc)
    {
        Status = BillingInvoiceStatus.Paid;
        PaidAtUtc = paidAtUtc;
    }

    public void MarkOpen()
    {
        Status = BillingInvoiceStatus.Open;
    }
}
