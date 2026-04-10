using SocioTorcedor.BuildingBlocks.Application.Payments;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Services;

/// <summary>
/// Provider de pagamento fake para desenvolvimento/MVP (substituir por Asaas/Pagar.me/Mercado Pago).
/// </summary>
public sealed class StubPaymentProvider : IPaymentProvider
{
    public Task CancelAsync(
        PaymentProviderContext context,
        string externalSubscriptionId,
        CancellationToken cancellationToken = default)
    {
        _ = context;
        _ = externalSubscriptionId;
        return Task.CompletedTask;
    }

    public Task<CreateCardChargeResult> CreateCardAsync(
        CreateCardChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        var id = $"card_{Guid.NewGuid():N}";
        return Task.FromResult(new CreateCardChargeResult(id, "succeeded"));
    }

    public Task<CreatePixChargeResult> CreatePixAsync(
        CreatePixChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        var id = $"pix_{Guid.NewGuid():N}";
        var payload =
            $"00020126580014br.gov.bcb.pix0136stub-{id}5204000053039865802BR5925STUB6009SAO PAULO62070503***6304ABCD";
        return Task.FromResult(new CreatePixChargeResult(
            id,
            payload,
            payload,
            DateTimeOffset.UtcNow.AddHours(1)));
    }

    public Task<CreateSubscriptionResult> CreateSubscriptionAsync(
        CreateSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var prefix = request.Context == PaymentProviderContext.SaaS ? "saas" : "mem";
        var customerId = $"{prefix}_cust_{Guid.NewGuid():N}";
        var subId = $"{prefix}_sub_{Guid.NewGuid():N}";
        return Task.FromResult(new CreateSubscriptionResult(customerId, subId, "active"));
    }

    public Task<PaymentProviderStatusResult> GetStatusAsync(
        PaymentProviderContext context,
        string externalId,
        CancellationToken cancellationToken = default)
    {
        _ = context;
        return Task.FromResult(new PaymentProviderStatusResult(externalId, "unknown", null));
    }
}
