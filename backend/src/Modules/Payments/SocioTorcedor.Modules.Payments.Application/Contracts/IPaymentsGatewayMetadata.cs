namespace SocioTorcedor.Modules.Payments.Application.Contracts;

/// <summary>
/// Indica se o gateway real Stripe está habilitado (configurado com secret key).
/// </summary>
public interface IPaymentsGatewayMetadata
{
    bool IsStripeEnabled { get; }
}
