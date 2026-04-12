using SocioTorcedor.BuildingBlocks.Application.Payments;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Payments.Infrastructure.Options;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Services;

/// <summary>
/// Roteia chamadas do <see cref="IPaymentProvider"/>: Billing SaaS na conta da plataforma;
/// cobrança de sócio na conta Stripe do tenant (chaves diretas) ou stub.
/// </summary>
public sealed class RoutingPaymentProvider(
    StripePaymentProvider platformStripe,
    StubPaymentProvider stubPaymentProvider,
    ICurrentTenantContext tenantContext,
    MemberStripeOperationsResolver memberStripeResolver,
    Microsoft.Extensions.Options.IOptions<PaymentsOptions> paymentsOptions) : IPaymentProvider
{
    private readonly PaymentsOptions _paymentsOptions = paymentsOptions.Value;

    public async Task<CreateSubscriptionResult> CreateSubscriptionAsync(
        CreateSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Context == PaymentProviderContext.SaaS)
            return await platformStripe.Operations.CreateSubscriptionAsync(request, cancellationToken);

        if (request.Context != PaymentProviderContext.Member)
            throw new InvalidOperationException($"Unsupported context {request.Context}.");

        if (!tenantContext.IsResolved)
            throw new InvalidOperationException("Tenant must be resolved for member payment operations.");

        var ops = await memberStripeResolver.TryResolveAsync(tenantContext.TenantId, cancellationToken);
        if (ops is not null)
        {
            var req = request with { ConnectedAccountId = null };
            return await ops.CreateSubscriptionAsync(req, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(_paymentsOptions.StripeSecretKey))
            return await stubPaymentProvider.CreateSubscriptionAsync(request, cancellationToken);

        throw new InvalidOperationException(
            "Member payment gateway is not configured for this tenant. Configure Stripe API keys in admin.");
    }

    public async Task<CreatePixChargeResult> CreatePixAsync(
        CreatePixChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Context == PaymentProviderContext.SaaS)
            throw new NotSupportedException("PIX SaaS is not used.");

        if (!tenantContext.IsResolved)
            throw new InvalidOperationException("Tenant must be resolved for member payment operations.");

        var ops = await memberStripeResolver.TryResolveAsync(tenantContext.TenantId, cancellationToken);
        if (ops is not null)
        {
            var req = request with { ConnectedAccountId = null };
            return await ops.CreatePixAsync(req, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(_paymentsOptions.StripeSecretKey))
            return await stubPaymentProvider.CreatePixAsync(request, cancellationToken);

        throw new InvalidOperationException(
            "Member payment gateway is not configured for this tenant. Configure Stripe API keys in admin.");
    }

    public Task<CreateCardChargeResult> CreateCardAsync(
        CreateCardChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Context == PaymentProviderContext.SaaS)
            return platformStripe.Operations.CreateCardAsync(request, cancellationToken);

        return stubPaymentProvider.CreateCardAsync(request, cancellationToken);
    }

    public async Task CancelAsync(
        PaymentProviderContext context,
        string externalSubscriptionId,
        string? connectedAccountId = null,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        if (context == PaymentProviderContext.SaaS)
        {
            await platformStripe.Operations.CancelAsync(context, externalSubscriptionId, connectedAccountId, idempotencyKey, cancellationToken);
            return;
        }

        if (!tenantContext.IsResolved)
            throw new InvalidOperationException("Tenant must be resolved for member payment operations.");

        var ops = await memberStripeResolver.TryResolveAsync(tenantContext.TenantId, cancellationToken);
        if (ops is not null)
        {
            await ops.CancelAsync(context, externalSubscriptionId, null, idempotencyKey, cancellationToken);
            return;
        }

        await stubPaymentProvider.CancelAsync(context, externalSubscriptionId, null, idempotencyKey, cancellationToken);
    }

    public async Task<PaymentProviderStatusResult> GetStatusAsync(
        PaymentProviderContext context,
        string externalId,
        CancellationToken cancellationToken = default)
    {
        if (context == PaymentProviderContext.SaaS)
            return await platformStripe.Operations.GetStatusAsync(context, externalId, cancellationToken);

        if (!tenantContext.IsResolved)
            throw new InvalidOperationException("Tenant must be resolved for member payment operations.");

        var ops = await memberStripeResolver.TryResolveAsync(tenantContext.TenantId, cancellationToken);
        if (ops is not null)
            return await ops.GetStatusAsync(context, externalId, cancellationToken);

        return await stubPaymentProvider.GetStatusAsync(context, externalId, cancellationToken);
    }

    public async Task<CreateCheckoutSessionResult> CreateCheckoutSessionAsync(
        CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Context == PaymentProviderContext.SaaS)
            throw new NotSupportedException("Checkout SaaS is not used.");

        if (!tenantContext.IsResolved)
            throw new InvalidOperationException("Tenant must be resolved for member payment operations.");

        var ops = await memberStripeResolver.TryResolveAsync(tenantContext.TenantId, cancellationToken);
        if (ops is not null)
        {
            var req = request with { ConnectedAccountId = null };
            return await ops.CreateCheckoutSessionAsync(req, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(_paymentsOptions.StripeSecretKey))
            return await stubPaymentProvider.CreateCheckoutSessionAsync(request, cancellationToken);

        throw new InvalidOperationException(
            "Member payment gateway is not configured for this tenant. Configure Stripe API keys in admin.");
    }

    public Task<CreateBillingPortalSessionResult> CreateBillingPortalSessionAsync(
        CreateBillingPortalSessionRequest request,
        CancellationToken cancellationToken = default) =>
        platformStripe.Operations.CreateBillingPortalSessionAsync(request, cancellationToken);

    public Task<ListSaasCustomerPaymentMethodsResult> ListSaasCustomerPaymentMethodsAsync(
        ListSaasCustomerPaymentMethodsRequest request,
        CancellationToken cancellationToken = default) =>
        platformStripe.Operations.ListSaasCustomerPaymentMethodsAsync(request, cancellationToken);

    public Task<CreateSaasSetupIntentResult> CreateSaasSetupIntentAsync(
        CreateSaasSetupIntentRequest request,
        CancellationToken cancellationToken = default) =>
        platformStripe.Operations.CreateSaasSetupIntentAsync(request, cancellationToken);

    public Task AttachSaasPaymentMethodAsync(
        AttachSaasPaymentMethodRequest request,
        CancellationToken cancellationToken = default) =>
        platformStripe.Operations.AttachSaasPaymentMethodAsync(request, cancellationToken);

    public Task DetachSaasPaymentMethodAsync(
        DetachSaasPaymentMethodRequest request,
        CancellationToken cancellationToken = default) =>
        platformStripe.Operations.DetachSaasPaymentMethodAsync(request, cancellationToken);
}
