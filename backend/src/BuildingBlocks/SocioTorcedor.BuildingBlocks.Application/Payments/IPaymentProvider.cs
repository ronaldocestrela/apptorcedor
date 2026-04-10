namespace SocioTorcedor.BuildingBlocks.Application.Payments;

/// <summary>
/// Abstração de gateway de pagamento (Asaas, Pagar.me, Mercado Pago, etc.).
/// Implementações concretas ficam na infraestrutura; o MVP usa um provider "stub".
/// </summary>
public interface IPaymentProvider
{
    Task<CreateSubscriptionResult> CreateSubscriptionAsync(
        CreateSubscriptionRequest request,
        CancellationToken cancellationToken = default);

    Task<CreatePixChargeResult> CreatePixAsync(
        CreatePixChargeRequest request,
        CancellationToken cancellationToken = default);

    Task<CreateCardChargeResult> CreateCardAsync(
        CreateCardChargeRequest request,
        CancellationToken cancellationToken = default);

    Task CancelAsync(
        PaymentProviderContext context,
        string externalSubscriptionId,
        CancellationToken cancellationToken = default);

    Task<PaymentProviderStatusResult> GetStatusAsync(
        PaymentProviderContext context,
        string externalId,
        CancellationToken cancellationToken = default);
}
