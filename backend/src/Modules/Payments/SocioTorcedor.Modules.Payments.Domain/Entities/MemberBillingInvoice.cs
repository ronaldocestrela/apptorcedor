using SocioTorcedor.BuildingBlocks.Domain.Abstractions;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Domain.Entities;

public sealed class MemberBillingInvoice : AggregateRoot
{
    private MemberBillingInvoice()
    {
    }

    public Guid MemberBillingSubscriptionId { get; private set; }

    public MemberBillingSubscription? Subscription { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = "BRL";

    public PaymentMethodKind PaymentMethod { get; private set; }

    public DateTime DueAtUtc { get; private set; }

    public BillingInvoiceStatus Status { get; private set; }

    public string? ExternalInvoiceId { get; private set; }

    public string? PixCopyPaste { get; private set; }

    public DateTime? PaidAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static MemberBillingInvoice Create(
        Guid subscriptionId,
        decimal amount,
        string currency,
        PaymentMethodKind paymentMethod,
        DateTime dueAtUtc,
        BillingInvoiceStatus status,
        string? externalInvoiceId,
        string? pixCopyPaste)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        var now = DateTime.UtcNow;
        return new MemberBillingInvoice
        {
            MemberBillingSubscriptionId = subscriptionId,
            Amount = amount,
            Currency = string.IsNullOrWhiteSpace(currency) ? "BRL" : currency.Trim().ToUpperInvariant(),
            PaymentMethod = paymentMethod,
            DueAtUtc = dueAtUtc,
            Status = status,
            ExternalInvoiceId = externalInvoiceId,
            PixCopyPaste = pixCopyPaste,
            CreatedAtUtc = now
        };
    }

    public void MarkPaid(DateTime paidAtUtc)
    {
        Status = BillingInvoiceStatus.Paid;
        PaidAtUtc = paidAtUtc;
    }
}
