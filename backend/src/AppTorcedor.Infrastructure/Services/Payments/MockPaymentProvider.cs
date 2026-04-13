using AppTorcedor.Application.Abstractions;

namespace AppTorcedor.Infrastructure.Services.Payments;

/// <summary>Deterministic no-op provider for B.6 until a real gateway is wired.</summary>
public sealed class MockPaymentProvider : IPaymentProvider
{
    public Task CreateSubscriptionAsync(Guid paymentId, decimal amount, string currency, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task CreatePixAsync(Guid paymentId, decimal amount, string currency, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task CreateCardAsync(Guid paymentId, decimal amount, string currency, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task CancelAsync(Guid paymentId, string? externalReference, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task RefundAsync(Guid paymentId, string? externalReference, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
