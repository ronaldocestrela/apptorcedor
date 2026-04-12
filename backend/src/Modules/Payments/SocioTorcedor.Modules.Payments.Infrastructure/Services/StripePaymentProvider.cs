using Microsoft.Extensions.Options;
using SocioTorcedor.BuildingBlocks.Application.Payments;
using SocioTorcedor.Modules.Payments.Infrastructure.Options;
using Stripe;
using Stripe.Checkout;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Services;

/// <summary>
/// Implementação Stripe (Billing SaaS na plataforma + Connect Express nos tenants).
/// </summary>
public sealed class StripePaymentProvider(IOptions<PaymentsOptions> options) : IPaymentProvider
{
    private readonly PaymentsOptions _options = options.Value;

    private StripeClient Client =>
        new(_options.StripeSecretKey ?? throw new InvalidOperationException("Payments:StripeSecretKey is not configured."));

    private static RequestOptions Req(string? idempotencyKey = null, string? stripeAccount = null)
    {
        var ro = new RequestOptions();
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            ro.IdempotencyKey = idempotencyKey;
        if (!string.IsNullOrWhiteSpace(stripeAccount))
            ro.StripeAccount = stripeAccount;
        return ro;
    }

    public async Task CancelAsync(
        PaymentProviderContext context,
        string externalSubscriptionId,
        string? connectedAccountId = null,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        _ = context;
        if (!ShouldInvokeStripeSubscriptionCancel(externalSubscriptionId))
            return;

        var subscriptionId = externalSubscriptionId.Trim();
        try
        {
            var service = new SubscriptionService(Client);
            await service.CancelAsync(
                subscriptionId,
                new SubscriptionCancelOptions(),
                Req(idempotencyKey, connectedAccountId),
                cancellationToken);
        }
        catch (StripeException ex) when (IsMissingSubscriptionStripeError(ex))
        {
            // Assinatura já não existe na Stripe (cancelamento idempotente ou ID obsoleto).
        }
    }

    /// <summary>
    /// Somente IDs com prefixo <c>sub_</c> são cancelados via API Stripe.
    /// IDs legados (ex.: <c>mem_sub_*</c> do stub) são ignorados.
    /// </summary>
    internal static bool ShouldInvokeStripeSubscriptionCancel(string? externalSubscriptionId)
    {
        if (string.IsNullOrWhiteSpace(externalSubscriptionId))
            return false;

        return externalSubscriptionId.Trim().StartsWith("sub_", StringComparison.Ordinal);
    }

    internal static bool IsMissingSubscriptionStripeError(StripeException ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        if (string.Equals(ex.StripeError?.Code, "resource_missing", StringComparison.OrdinalIgnoreCase))
            return true;

        if (ex.Message.Contains("No such subscription", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    public async Task<CreateBillingPortalSessionResult> CreateBillingPortalSessionAsync(
        CreateBillingPortalSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var service = new Stripe.BillingPortal.SessionService(Client);
        var session = await service.CreateAsync(
            new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = request.CustomerId,
                ReturnUrl = request.ReturnUrl
            },
            Req(request.IdempotencyKey),
            cancellationToken);

        return new CreateBillingPortalSessionResult(session.Url);
    }

    public Task<CreateCardChargeResult> CreateCardAsync(
        CreateCardChargeRequest request,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Card charges are not implemented for Stripe; use Checkout.");

    public async Task<CreateCheckoutSessionResult> CreateCheckoutSessionAsync(
        CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var mode = request.Mode.Equals("payment", StringComparison.OrdinalIgnoreCase) ? "payment" : "subscription";
        var lineItem = new SessionLineItemOptions
        {
            Quantity = 1,
            PriceData = new SessionLineItemPriceDataOptions
            {
                Currency = request.Currency.ToLowerInvariant(),
                UnitAmountDecimal = request.Amount * 100m,
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = request.ProductName
                },
                Recurring = mode == "subscription"
                    ? new SessionLineItemPriceDataRecurringOptions
                    {
                        Interval = request.BillingInterval.Equals("year", StringComparison.OrdinalIgnoreCase)
                            ? "year"
                            : "month"
                    }
                    : null
            }
        };

        var sessionOptions = new SessionCreateOptions
        {
            Mode = mode,
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            LineItems = new List<SessionLineItemOptions> { lineItem },
            Metadata = request.Metadata.ToDictionary(static x => x.Key, static x => x.Value)
        };

        if (!string.IsNullOrWhiteSpace(request.CustomerEmail))
            sessionOptions.CustomerEmail = request.CustomerEmail;

        if (mode == "payment" && string.Equals(request.Currency, "BRL", StringComparison.OrdinalIgnoreCase))
            sessionOptions.PaymentMethodTypes = new List<string> { "card", "pix" };

        if (mode == "subscription")
        {
            sessionOptions.SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = request.Metadata.ToDictionary(static x => x.Key, static x => x.Value)
            };
        }

        var service = new SessionService(Client);
        var session = await service.CreateAsync(sessionOptions, Req(request.IdempotencyKey, request.ConnectedAccountId), cancellationToken);
        return new CreateCheckoutSessionResult(session.Id, session.Url);
    }

    public async Task<CreateConnectAccountLinkResult> CreateConnectAccountLinkAsync(
        CreateConnectAccountLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        var service = new AccountLinkService(Client);
        var link = await service.CreateAsync(
            new AccountLinkCreateOptions
            {
                Account = request.AccountId,
                RefreshUrl = request.RefreshUrl,
                ReturnUrl = request.ReturnUrl,
                Type = "account_onboarding"
            },
            Req(request.IdempotencyKey),
            cancellationToken);

        return new CreateConnectAccountLinkResult(link.Url);
    }

    public async Task<CreateConnectExpressAccountResult> CreateConnectExpressAccountAsync(
        CreateConnectExpressAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var service = new AccountService(Client);
        var account = await service.CreateAsync(
            new AccountCreateOptions
            {
                Type = "express",
                Country = request.Country,
                Email = request.Email,
                Metadata = request.Metadata.ToDictionary(static x => x.Key, static x => x.Value),
                Capabilities = new AccountCapabilitiesOptions
                {
                    CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                    Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
                }
            },
            Req(request.IdempotencyKey),
            cancellationToken);

        return new CreateConnectExpressAccountResult(account.Id);
    }

    public async Task<ConnectAccountStatusResult> GetConnectAccountStatusAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        var service = new AccountService(Client);
        var account = await service.GetAsync(accountId, cancellationToken: cancellationToken);
        return new ConnectAccountStatusResult(
            account.Id,
            account.ChargesEnabled,
            account.PayoutsEnabled,
            account.DetailsSubmitted);
    }

    public async Task<CreatePixChargeResult> CreatePixAsync(
        CreatePixChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        var service = new PaymentIntentService(Client);
        var pi = await service.CreateAsync(
            new PaymentIntentCreateOptions
            {
                Amount = (long)Math.Round(request.Amount * 100m, MidpointRounding.AwayFromZero),
                Currency = request.Currency.ToLowerInvariant(),
                PaymentMethodTypes = new List<string> { "pix" },
                Description = request.Description,
                Metadata = new Dictionary<string, string>
                {
                    ["reference"] = request.Reference
                }
            },
            Req(idempotencyKey: $"pix:{request.Reference}", stripeAccount: request.ConnectedAccountId),
            cancellationToken);

        var copyPaste = pi.ClientSecret ?? pi.Id;
        return new CreatePixChargeResult(
            pi.Id,
            copyPaste,
            copyPaste,
            DateTimeOffset.UtcNow.AddHours(1));
    }

    public async Task<CreateSubscriptionResult> CreateSubscriptionAsync(
        CreateSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var priceId = await ResolveSubscriptionPriceIdAsync(request, cancellationToken);

        var customerService = new CustomerService(Client);
        var customer = await customerService.CreateAsync(
            new CustomerCreateOptions
            {
                Email = request.CustomerEmail,
                Metadata = new Dictionary<string, string>
                {
                    ["reference"] = request.TenantOrMemberReference
                }
            },
            Req($"{request.IdempotencyKey}:cust", request.ConnectedAccountId),
            cancellationToken);

        var subscriptionService = new SubscriptionService(Client);
        var sub = await subscriptionService.CreateAsync(
            new SubscriptionCreateOptions
            {
                Customer = customer.Id,
                Items = new List<SubscriptionItemOptions> { new() { Price = priceId } },
                Metadata = new Dictionary<string, string>
                {
                    ["reference"] = request.TenantOrMemberReference
                }
            },
            Req($"{request.IdempotencyKey}:sub", request.ConnectedAccountId),
            cancellationToken);

        return new CreateSubscriptionResult(customer.Id, sub.Id, sub.Status, null);
    }

    public async Task<PaymentProviderStatusResult> GetStatusAsync(
        PaymentProviderContext context,
        string externalId,
        CancellationToken cancellationToken = default)
    {
        _ = context;
        var service = new SubscriptionService(Client);
        var sub = await service.GetAsync(externalId, cancellationToken: cancellationToken);
        return new PaymentProviderStatusResult(sub.Id, sub.Status, sub.Status);
    }

    private async Task<string> ResolveSubscriptionPriceIdAsync(
        CreateSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.StripePriceId))
            return request.StripePriceId!;

        var productService = new ProductService(Client);
        var product = await productService.CreateAsync(
            new ProductCreateOptions
            {
                Name = request.ProductName ?? "Subscription"
            },
            Req($"{request.IdempotencyKey}:prod", request.ConnectedAccountId),
            cancellationToken);

        var priceService = new PriceService(Client);
        var price = await priceService.CreateAsync(
            new PriceCreateOptions
            {
                Product = product.Id,
                Currency = request.Currency.ToLowerInvariant(),
                UnitAmountDecimal = request.Amount * 100m,
                Recurring = new PriceRecurringOptions
                {
                    Interval = request.BillingInterval.Equals("year", StringComparison.OrdinalIgnoreCase)
                        ? "year"
                        : "month"
                }
            },
            Req($"{request.IdempotencyKey}:price", request.ConnectedAccountId),
            cancellationToken);

        return price.Id;
    }

    public async Task<ListSaasCustomerPaymentMethodsResult> ListSaasCustomerPaymentMethodsAsync(
        ListSaasCustomerPaymentMethodsRequest request,
        CancellationToken cancellationToken = default)
    {
        var customerService = new Stripe.CustomerService(Client);
        var customer = await customerService.GetAsync(request.CustomerId, cancellationToken: cancellationToken);
        var defaultPmId = customer.InvoiceSettings?.DefaultPaymentMethodId;

        var pmService = new Stripe.PaymentMethodService(Client);
        var list = await pmService.ListAsync(
            new Stripe.PaymentMethodListOptions
            {
                Customer = request.CustomerId,
                Type = "card"
            },
            cancellationToken: cancellationToken);

        var items = list.Data.Select(pm => new SaasPaymentMethodListItem(
            pm.Id,
            pm.Card?.Brand ?? "card",
            pm.Card?.Last4 ?? "0000",
            (int)(pm.Card?.ExpMonth ?? 0),
            (int)(pm.Card?.ExpYear ?? 0),
            string.Equals(pm.Id, defaultPmId, StringComparison.Ordinal))).ToList();

        return new ListSaasCustomerPaymentMethodsResult(items);
    }

    public async Task<CreateSaasSetupIntentResult> CreateSaasSetupIntentAsync(
        CreateSaasSetupIntentRequest request,
        CancellationToken cancellationToken = default)
    {
        var service = new Stripe.SetupIntentService(Client);
        var si = await service.CreateAsync(
            new Stripe.SetupIntentCreateOptions
            {
                Customer = request.CustomerId,
                PaymentMethodTypes = new List<string> { "card" },
                Usage = "off_session"
            },
            Req(request.IdempotencyKey),
            cancellationToken);

        if (string.IsNullOrWhiteSpace(si.ClientSecret))
            throw new InvalidOperationException("Stripe SetupIntent returned no client secret.");

        return new CreateSaasSetupIntentResult(si.ClientSecret, si.Id);
    }

    public async Task AttachSaasPaymentMethodAsync(
        AttachSaasPaymentMethodRequest request,
        CancellationToken cancellationToken = default)
    {
        var pmService = new Stripe.PaymentMethodService(Client);
        await pmService.AttachAsync(
            request.PaymentMethodId,
            new Stripe.PaymentMethodAttachOptions { Customer = request.CustomerId },
            Req($"{request.IdempotencyKey}:attach"),
            cancellationToken);

        if (!request.SetAsDefault)
            return;

        var customerService = new Stripe.CustomerService(Client);
        await customerService.UpdateAsync(
            request.CustomerId,
            new Stripe.CustomerUpdateOptions
            {
                InvoiceSettings = new Stripe.CustomerInvoiceSettingsOptions
                {
                    DefaultPaymentMethod = request.PaymentMethodId
                }
            },
            Req($"{request.IdempotencyKey}:custdef"),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.ExternalSubscriptionId))
        {
            var subService = new Stripe.SubscriptionService(Client);
            await subService.UpdateAsync(
                request.ExternalSubscriptionId,
                new Stripe.SubscriptionUpdateOptions
                {
                    DefaultPaymentMethod = request.PaymentMethodId
                },
                Req($"{request.IdempotencyKey}:subdef"),
                cancellationToken);
        }
    }

    public async Task DetachSaasPaymentMethodAsync(
        DetachSaasPaymentMethodRequest request,
        CancellationToken cancellationToken = default)
    {
        var pmService = new Stripe.PaymentMethodService(Client);
        await pmService.DetachAsync(request.PaymentMethodId, cancellationToken: cancellationToken);
    }
}
