namespace SocioTorcedor.Modules.Payments.Domain.Enums;

public enum BillingInvoiceStatus
{
    Draft = 0,
    Open = 1,
    Paid = 2,
    Void = 3,
    Uncollectible = 4
}
