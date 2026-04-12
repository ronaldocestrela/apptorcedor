using SocioTorcedor.BuildingBlocks.Application.Payments;
using Stripe;
using Stripe.Checkout;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Services;

/// <summary>
/// Operações Stripe parametrizadas por <see cref="StripeClient"/> (conta plataforma ou conta do tenant).
/// </summary>
public sealed class StripePaymentOperations(StripeClient client)
{
    private readonly StripeClient _client = client;

    public static bool ShouldInvokeStripeSubscriptionCancel(string? externalSubscriptionId)
    {
        if (string.IsNullOrWhiteSpace(externalSubscriptionId))
            return false;

        return externalSubscriptionId.Trim().StartsWith("sub_", StringComparison.Ordinal);
    }

    public static bool IsMissingSubscriptionStripeError(StripeException ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        if (string.Equals(ex.StripeError?.Code, "resource_missing", StringComparison.OrdinalIgnoreCase))
            return true;

        if (ex.Message.Contains("No such subscription", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

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
            var service = new SubscriptionService(_client);
            await service.CancelAsync(
                subscriptionId,
                new SubscriptionCancelOptions(),
                Req(idempotencyKey, connectedAccountId),
                cancellationToken);
        }
        catch (StripeException ex) when (IsMissingSubscriptionStripeError(ex))
        {
        }
    }

    public async Task<CreateBillingPortalSessionResult> CreateBillingPortalSessionAsync(
        CreateBillingPortalSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var service = new Stripe.BillingPortal.SessionService(_client);
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

        var service = new SessionService(_client);
        var session = await service.CreateAsync(sessionOptions, Req(request.IdempotencyKey, request.ConnectedAccountId), cancellationToken);
        return new CreateCheckoutSessionResult(session.Id, session.Url);
    }

    public async Task<CreatePixChargeResult> CreatePixAsync(
        CreatePixChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        var service = new PaymentIntentService(_client);
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

        var meta = new Dictionary<string, string>
        {
            ["reference"] = request.TenantOrMemberReference
        };
        if (request.AdditionalMetadata is not null)
        {
            foreach (var kv in request.AdditionalMetadata!)
                meta[kv.Key] = kv.Value;
        }

        var customerService = new CustomerService(_client);
        var customer = await customerService.CreateAsync(
            new CustomerCreateOptions
            {
                Email = request.CustomerEmail,
                Metadata = meta
            },
            Req($"{request.IdempotencyKey}:cust", request.ConnectedAccountId),
            cancellationToken);

        var subscriptionService = new SubscriptionService(_client);
        var sub = await subscriptionService.CreateAsync(
            new SubscriptionCreateOptions
            {
                Customer = customer.Id,
                Items = new List<SubscriptionItemOptions> { new() { Price = priceId } },
                Metadata = meta
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
        var service = new SubscriptionService(_client);
        var sub = await service.GetAsync(externalId, cancellationToken: cancellationToken);
        return new PaymentProviderStatusResult(sub.Id, sub.Status, sub.Status);
    }

    private async Task<string> ResolveSubscriptionPriceIdAsync(
        CreateSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.StripePriceId))
            return request.StripePriceId!;

        var productService = new ProductService(_client);
        var product = await productService.CreateAsync(
            new ProductCreateOptions
            {
                Name = request.ProductName ?? "Subscription"
            },
            Req($"{request.IdempotencyKey}:prod", request.ConnectedAccountId),
            cancellationToken);

        var priceService = new PriceService(_client);
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
        var customerService = new Stripe.CustomerService(_client);
        var customer = await customerService.GetAsync(request.CustomerId, cancellationToken: cancellationToken);
        var defaultPmId = customer.InvoiceSettings?.DefaultPaymentMethodId;

        var pmService = new Stripe.PaymentMethodService(_client);
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
        var service = new Stripe.SetupIntentService(_client);
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
        var pmService = new Stripe.PaymentMethodService(_client);
        await pmService.AttachAsync(
            request.PaymentMethodId,
            new Stripe.PaymentMethodAttachOptions { Customer = request.CustomerId },
            Req($"{request.IdempotencyKey}:attach"),
            cancellationToken);

        if (!request.SetAsDefault)
            return;

        var customerService = new Stripe.CustomerService(_client);
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
            var subService = new Stripe.SubscriptionService(_client);
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
        var pmService = new Stripe.PaymentMethodService(_client);
        await pmService.DetachAsync(request.PaymentMethodId, cancellationToken: cancellationToken);
    }
}
