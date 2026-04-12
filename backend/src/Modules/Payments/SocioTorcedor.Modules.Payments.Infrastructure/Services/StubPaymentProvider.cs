using System.Collections.Concurrent;
using SocioTorcedor.BuildingBlocks.Application.Payments;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Services;

/// <summary>
/// Provider de pagamento fake para desenvolvimento/MVP (substituir por Asaas/Pagar.me/Mercado Pago).
/// </summary>
public sealed class StubPaymentProvider : IPaymentProvider
{
    private static readonly ConcurrentDictionary<string, StubSaasCardStore> SaasCardsByCustomer = new();

    private sealed class StubSaasCardStore
    {
        public string? DefaultPaymentMethodId { get; set; }

        public List<SaasPaymentMethodListItem> Items { get; } = new();
    }
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

    public Task<ListSaasCustomerPaymentMethodsResult> ListSaasCustomerPaymentMethodsAsync(
        ListSaasCustomerPaymentMethodsRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var store = SaasCardsByCustomer.GetOrAdd(request.CustomerId, _ => new StubSaasCardStore());
        lock (store.Items)
        {
            var items = store.Items
                .Select(pm => pm with
                {
                    IsDefault = string.Equals(pm.Id, store.DefaultPaymentMethodId, StringComparison.Ordinal)
                })
                .ToList();

            return Task.FromResult(new ListSaasCustomerPaymentMethodsResult(items));
        }
    }

    public Task<CreateSaasSetupIntentResult> CreateSaasSetupIntentAsync(
        CreateSaasSetupIntentRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var id = $"seti_stub_{Guid.NewGuid():N}";
        return Task.FromResult(new CreateSaasSetupIntentResult(
            $"stub_secret_{id}",
            id));
    }

    public Task AttachSaasPaymentMethodAsync(
        AttachSaasPaymentMethodRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var store = SaasCardsByCustomer.GetOrAdd(request.CustomerId, _ => new StubSaasCardStore());
        lock (store.Items)
        {
            if (!store.Items.Any(x => x.Id == request.PaymentMethodId))
            {
                var pm = new SaasPaymentMethodListItem(
                    request.PaymentMethodId,
                    "visa",
                    "4242",
                    12,
                    2030,
                    false);
                store.Items.Add(pm);
            }

            if (request.SetAsDefault)
            {
                store.DefaultPaymentMethodId = request.PaymentMethodId;
                for (var i = 0; i < store.Items.Count; i++)
                {
                    var x = store.Items[i];
                    store.Items[i] = x with
                    {
                        IsDefault = x.Id == request.PaymentMethodId
                    };
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task DetachSaasPaymentMethodAsync(
        DetachSaasPaymentMethodRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (!SaasCardsByCustomer.TryGetValue(request.CustomerId, out var store))
            return Task.CompletedTask;

        lock (store.Items)
        {
            store.Items.RemoveAll(x => x.Id == request.PaymentMethodId);
            if (string.Equals(store.DefaultPaymentMethodId, request.PaymentMethodId, StringComparison.Ordinal))
                store.DefaultPaymentMethodId = store.Items.Count > 0 ? store.Items[0].Id : null;
        }

        return Task.CompletedTask;
    }
}
