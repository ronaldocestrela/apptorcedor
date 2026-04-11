namespace SocioTorcedor.Modules.Payments.Application.Options;

/// <summary>
/// Comportamento de processamento de webhooks Stripe (ligado à seção <c>Payments</c> no configuration).
/// </summary>
public sealed class StripeWebhookHandlingOptions
{
    public const string SectionName = "Payments";

    /// <summary>
    /// Quando true, valida assinatura e resolve thin events mas não altera o domínio (apenas logs).
    /// </summary>
    public bool StripeWebhookShadowMode { get; set; }
}
