using AppTorcedor.Application.Abstractions;

namespace AppTorcedor.Infrastructure.Services.Payments;

/// <summary>Deterministic mock provider for B.6 / D.4 until a real gateway is wired.</summary>
public sealed class MockPaymentProvider : IPaymentProvider
{
    public string ProviderKey => "Mock";

    public Task CreateSubscriptionAsync(Guid paymentId, decimal amount, string currency, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<PixPaymentProviderResult> CreatePixAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(
            new PixPaymentProviderResult(
                QrCodePayload: $"MOCK_PIX|{paymentId:N}|{amount:F2}|{currency}",
                CopyPasteKey: $"00020126{paymentId:N}520400005303986540{amount:F2}5802BR5925MOCK MERCHANT6009SAO PAULO62070503***6304ABCD"));

    public Task<CardPaymentProviderResult> CreateCardAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(
            new CardPaymentProviderResult(
                CheckoutUrl: $"https://mock-payments.local/checkout/{paymentId:N}?amount={amount:F2}&currency={currency}"));

    public Task CancelAsync(Guid paymentId, string? externalReference, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task RefundAsync(Guid paymentId, string? externalReference, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
