namespace SocioTorcedor.BuildingBlocks.Application.Payments;

/// <summary>
/// Identifies which billing boundary is calling the payment provider (SaaS vs. sócio no tenant).
/// </summary>
public enum PaymentProviderContext
{
    SaaS = 0,
    Member = 1
}
