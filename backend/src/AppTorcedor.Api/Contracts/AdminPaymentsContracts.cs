namespace AppTorcedor.Api.Contracts;

public sealed class PaymentConciliateRequest
{
    public DateTimeOffset? PaidAt { get; set; }
}

public sealed class PaymentCancelRequest
{
    public string? Reason { get; set; }
}

public sealed class PaymentRefundRequest
{
    public string? Reason { get; set; }
}
