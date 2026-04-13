namespace AppTorcedor.Application.Abstractions;

/// <summary>Gateway abstraction for PIX, card and recurring charges (mock implementation in B.6).</summary>
public interface IPaymentProvider
{
    Task CreateSubscriptionAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default);

    Task CreatePixAsync(Guid paymentId, decimal amount, string currency, CancellationToken cancellationToken = default);

    Task CreateCardAsync(Guid paymentId, decimal amount, string currency, CancellationToken cancellationToken = default);

    Task CancelAsync(Guid paymentId, string? externalReference, CancellationToken cancellationToken = default);

    Task RefundAsync(Guid paymentId, string? externalReference, CancellationToken cancellationToken = default);
}
