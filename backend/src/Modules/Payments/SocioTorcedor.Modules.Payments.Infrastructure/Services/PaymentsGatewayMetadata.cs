using Microsoft.Extensions.Options;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Infrastructure.Options;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Services;

public sealed class PaymentsGatewayMetadata(IOptions<PaymentsOptions> options) : IPaymentsGatewayMetadata
{
    public bool IsStripeEnabled => !string.IsNullOrWhiteSpace(options.Value.StripeSecretKey);
}
