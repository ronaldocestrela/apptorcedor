using SocioTorcedor.BuildingBlocks.Application.Payments;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Services;

/// <summary>
/// Roteia chamadas do <see cref="IPaymentProvider"/>: Billing SaaS na conta da plataforma;
/// cobrança de sócio na conta Stripe do tenant (chaves diretas).
/// </summary>
public sealed class RoutingPaymentProvider(
    StripePaymentProvider platformStripe,
    ICurrentTenantContext tenantContext,
    MemberStripeOperationsResolver memberStripeResolver) : IPaymentProvider
{
    private static InvalidOperationException MemberGatewayNotConfigured() =>
        new("Member payment gateway is not configured for this tenant. Configure Stripe Direct in admin.");

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
        if (ops is null)
            throw MemberGatewayNotConfigured();

        var req = request with { ConnectedAccountId = null };
        return await ops.CreateSubscriptionAsync(req, cancellationToken);
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
        if (ops is null)
            throw MemberGatewayNotConfigured();

        var req = request with { ConnectedAccountId = null };
        return await ops.CreatePixAsync(req, cancellationToken);
    }

    public Task<CreateCardChargeResult> CreateCardAsync(
        CreateCardChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Context == PaymentProviderContext.SaaS)
            return platformStripe.Operations.CreateCardAsync(request, cancellationToken);

        throw new NotSupportedException("Card charges for members are not implemented; use Checkout.");
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
        if (ops is null)
            throw MemberGatewayNotConfigured();

        await ops.CancelAsync(context, externalSubscriptionId, null, idempotencyKey, cancellationToken);
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
        if (ops is null)
            throw MemberGatewayNotConfigured();

        return await ops.GetStatusAsync(context, externalId, cancellationToken);
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
        if (ops is null)
            throw MemberGatewayNotConfigured();

        var req = request with { ConnectedAccountId = null };
        return await ops.CreateCheckoutSessionAsync(req, cancellationToken);
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
