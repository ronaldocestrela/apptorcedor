using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace AppTorcedor.Infrastructure.Services.Payments;

/// <summary>Stripe Checkout (one-time card payment) for D.4 / D.6.</summary>
public sealed class StripePaymentProvider(IOptions<PaymentsOptions> paymentsOptions) : IPaymentProvider
{
    public string ProviderKey => "Stripe";

    public Task CreateSubscriptionAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<PixPaymentProviderResult> CreatePixAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        Guid? payingUserId = null,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(
            "O provedor Stripe está habilitado apenas para pagamento com cartão nesta versão. Use cartão ou altere Payments:Provider.");

    public async Task<CardPaymentProviderResult> CreateCardAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        int? maxInstallments = null,
        CancellationToken cancellationToken = default)
    {
        var opts = paymentsOptions.Value.Stripe;
        var apiKey = opts.ApiKey?.Trim();
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Payments:Stripe:ApiKey não está configurada.");

        var successUrl = opts.SuccessUrl?.Trim();
        var cancelUrl = opts.CancelUrl?.Trim();
        if (string.IsNullOrEmpty(successUrl) || string.IsNullOrEmpty(cancelUrl))
            throw new InvalidOperationException("Payments:Stripe:SuccessUrl e CancelUrl são obrigatórias.");

        var client = new StripeClient(apiKey);
        var service = new SessionService(client);
        var unitAmount = (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
        if (unitAmount <= 0)
            throw new InvalidOperationException("Valor da cobrança inválido para Stripe Checkout.");

        var session = await service
            .CreateAsync(
                new SessionCreateOptions
                {
                    Mode = "payment",
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl,
                    ClientReferenceId = paymentId.ToString("D"),
                    Metadata = new Dictionary<string, string> { ["payment_id"] = paymentId.ToString("D") },
                    PaymentMethodTypes = ["card"],
                    PaymentMethodOptions = maxInstallments.HasValue
                        ? new SessionPaymentMethodOptionsOptions
                        {
                            Card = new SessionPaymentMethodOptionsCardOptions
                            {
                                Installments = new SessionPaymentMethodOptionsCardInstallmentsOptions
                                {
                                    Enabled = true,
                                },
                            },
                        }
                        : null,
                    LineItems =
                    [
                        new SessionLineItemOptions
                        {
                            Quantity = 1,
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = currency.Trim().ToLowerInvariant(),
                                UnitAmount = unitAmount,
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = "Assinatura sócio torcedor",
                                },
                            },
                        },
                    ],
                },
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return new CardPaymentProviderResult(session.Url ?? string.Empty, session.Id);
    }

    public async Task CancelAsync(Guid paymentId, string? externalReference, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalReference))
            return;

        var apiKey = paymentsOptions.Value.Stripe.ApiKey?.Trim();
        if (string.IsNullOrEmpty(apiKey))
            return;

        var client = new StripeClient(apiKey);
        try
        {
            if (externalReference.StartsWith("cs_", StringComparison.Ordinal))
            {
                var sessions = new SessionService(client);
                await sessions
                    .ExpireAsync(externalReference, options: null, requestOptions: null, cancellationToken)
                    .ConfigureAwait(false);
            }
            else if (externalReference.StartsWith("pi_", StringComparison.Ordinal))
            {
                var intents = new PaymentIntentService(client);
                await intents.CancelAsync(externalReference, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
        catch (StripeException)
        {
            // Session may already be expired / intent already canceled — treat as best-effort.
        }
    }

    public async Task RefundAsync(Guid paymentId, string? externalReference, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalReference) || !externalReference.StartsWith("pi_", StringComparison.Ordinal))
            return;

        var apiKey = paymentsOptions.Value.Stripe.ApiKey?.Trim();
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Payments:Stripe:ApiKey não está configurada.");

        var client = new StripeClient(apiKey);
        var refunds = new RefundService(client);
        await refunds
            .CreateAsync(
                new RefundCreateOptions { PaymentIntent = externalReference },
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
