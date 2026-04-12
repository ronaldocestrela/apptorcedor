namespace SocioTorcedor.Modules.Payments.Domain.Enums;

/// <summary>
/// Provedor de pagamento escolhido pelo backoffice para cobrança de sócios do tenant.
/// </summary>
public enum MemberPaymentProviderKind
{
    None = 0,
    StripeDirect = 1,
    Asaas = 2,
    MercadoPago = 3
}
