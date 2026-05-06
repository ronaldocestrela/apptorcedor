namespace AppTorcedor.Application.Abstractions;

/// <summary>Valores válidos para <see cref="TorcedorShirtRedemptionRequest.ShippingMethod"/> no resgate de camisa.</summary>
public static class TorcedorBenefitShippingMethods
{
    public const string Pickup = "pickup";

    public const string Carrier = "carrier";
}

public sealed record ShippingOptionDto(
    int ServiceId,
    string ServiceName,
    string CarrierName,
    string PictureUrl,
    decimal Price,
    int DeliveryDays);

/// <summary>Cotação de frete Melhor Envio (envio físico beneficiário).</summary>
public interface IMelhorEnvioShippingPort
{
    Task<IReadOnlyList<ShippingOptionDto>> CalculateAsync(string toCep, CancellationToken cancellationToken = default);
}
