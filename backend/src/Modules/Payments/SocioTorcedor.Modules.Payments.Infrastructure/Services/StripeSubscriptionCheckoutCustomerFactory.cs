using SocioTorcedor.BuildingBlocks.Application.Payments;
using Stripe;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Services;

/// <summary>
/// Monta opções de <see cref="Customer"/> para Checkout em modo subscription (Accounts V2).
/// </summary>
internal static class StripeSubscriptionCheckoutCustomerFactory
{
    internal static CustomerCreateOptions BuildCustomerCreateOptions(CreateCheckoutSessionRequest request)
    {
        var meta = request.Metadata.ToDictionary(static x => x.Key, static x => x.Value);
        var o = new CustomerCreateOptions { Metadata = meta };
        if (!string.IsNullOrWhiteSpace(request.CustomerEmail))
            o.Email = request.CustomerEmail.Trim();
        return o;
    }
}
