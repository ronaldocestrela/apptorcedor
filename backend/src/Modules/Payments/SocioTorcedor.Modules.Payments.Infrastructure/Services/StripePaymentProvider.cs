using Microsoft.Extensions.Options;
using SocioTorcedor.Modules.Payments.Infrastructure.Options;
using Stripe;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Services;

/// <summary>
/// Stripe na conta da plataforma (Billing SaaS, portal, cartões SaaS). Não implementa <see cref="IPaymentProvider"/> diretamente.
/// </summary>
public sealed class StripePaymentProvider
{
    public StripePaymentOperations Operations { get; }

    public StripePaymentProvider(IOptions<PaymentsOptions> options)
    {
        var k = options.Value.StripeSecretKey?.Trim();
        Operations = new StripePaymentOperations(
            string.IsNullOrWhiteSpace(k)
                ? new StripeClient("sk_test_placeholder")
                : new StripeClient(k));
    }
}
