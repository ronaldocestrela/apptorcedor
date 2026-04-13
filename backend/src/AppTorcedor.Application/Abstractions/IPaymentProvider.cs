namespace AppTorcedor.Application.Abstractions;

/// <summary>PIX instructions returned by <see cref="IPaymentProvider.CreatePixAsync"/>.</summary>
public sealed record PixPaymentProviderResult(string QrCodePayload, string? CopyPasteKey);

/// <summary>Hosted checkout URL returned by <see cref="IPaymentProvider.CreateCardAsync"/>.</summary>
public sealed record CardPaymentProviderResult(string CheckoutUrl);

/// <summary>Gateway abstraction for PIX, card and recurring charges (mock implementation in B.6).</summary>
public interface IPaymentProvider
{
    Task CreateSubscriptionAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default);

    Task<PixPaymentProviderResult> CreatePixAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default);

    Task<CardPaymentProviderResult> CreateCardAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default);

    Task CancelAsync(Guid paymentId, string? externalReference, CancellationToken cancellationToken = default);

    Task RefundAsync(Guid paymentId, string? externalReference, CancellationToken cancellationToken = default);
}
