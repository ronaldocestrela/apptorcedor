namespace AppTorcedor.Application.Modules.Administration.Payments;

/// <summary>Canonical payment charge lifecycle values persisted in <c>PaymentRecord.Status</c>.</summary>
public static class PaymentChargeStatuses
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Overdue = "Overdue";
    public const string Cancelled = "Cancelled";
    public const string Refunded = "Refunded";
}
