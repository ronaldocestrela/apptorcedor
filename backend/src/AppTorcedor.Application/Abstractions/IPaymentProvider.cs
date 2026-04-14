namespace AppTorcedor.Application.Abstractions;

/// <summary>PIX instructions returned by <see cref="IPaymentProvider.CreatePixAsync"/>.</summary>
public sealed record PixPaymentProviderResult(string QrCodePayload, string? CopyPasteKey);

/// <summary>Hosted checkout URL returned by <see cref="IPaymentProvider.CreateCardAsync"/>.</summary>
/// <param name="ProviderReference">Gateway reference for cancel/refund (e.g. Stripe <c>cs_</c> session id). When null, callers may fall back to the local payment id.</param>
public sealed record CardPaymentProviderResult(string CheckoutUrl, string? ProviderReference = null);

/// <summary>Gateway abstraction for PIX, card and recurring charges (mock implementation in B.6).</summary>
public interface IPaymentProvider
{
    /// <summary>Stored on <c>PaymentRecord.ProviderName</c> (e.g. Mock, Stripe).</summary>
    string ProviderKey { get; }

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
