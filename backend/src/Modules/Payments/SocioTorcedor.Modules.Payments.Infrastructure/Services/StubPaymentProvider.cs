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
        string? connectedAccountId = null,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        _ = context;
        _ = externalSubscriptionId;
        _ = connectedAccountId;
        _ = idempotencyKey;
        return Task.CompletedTask;
    }

    public Task<CreateBillingPortalSessionResult> CreateBillingPortalSessionAsync(
        CreateBillingPortalSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        return Task.FromResult(new CreateBillingPortalSessionResult(
            $"https://stub-billing-portal.example/{request.CustomerId}"));
    }

    public Task<CreateCardChargeResult> CreateCardAsync(
        CreateCardChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        var id = $"card_{Guid.NewGuid():N}";
        return Task.FromResult(new CreateCardChargeResult(id, "succeeded"));
    }

    public Task<CreateCheckoutSessionResult> CreateCheckoutSessionAsync(
        CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var id = $"cs_stub_{Guid.NewGuid():N}";
        return Task.FromResult(new CreateCheckoutSessionResult(
            id,
            $"https://stub-checkout.example/session/{id}"));
    }

    public Task<CreateConnectAccountLinkResult> CreateConnectAccountLinkAsync(
        CreateConnectAccountLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        return Task.FromResult(new CreateConnectAccountLinkResult(
            $"https://stub-connect.example/onboarding/{request.AccountId}"));
    }

    public Task<CreateConnectExpressAccountResult> CreateConnectExpressAccountAsync(
        CreateConnectExpressAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = request;
        _ = cancellationToken;
        return Task.FromResult(new CreateConnectExpressAccountResult($"acct_stub_{Guid.NewGuid():N}"));
    }

    public Task<ConnectAccountStatusResult> GetConnectAccountStatusAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        return Task.FromResult(new ConnectAccountStatusResult(accountId, true, true, true));
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
        return Task.FromResult(new CreateSubscriptionResult(customerId, subId, "active", null));
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
