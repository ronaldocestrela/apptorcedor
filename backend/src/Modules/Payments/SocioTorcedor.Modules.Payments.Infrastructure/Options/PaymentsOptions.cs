namespace SocioTorcedor.Modules.Payments.Infrastructure.Options;

public sealed class PaymentsOptions
{
    public const string SectionName = "Payments";

    /// <summary>Chave secreta da API Stripe (sk_test_... / sk_live_...).</summary>
    public string StripeSecretKey { get; set; } = string.Empty;

    /// <summary>Opcional: publishable key para o frontend (pk_...).</summary>
    public string StripePublishableKey { get; set; } = string.Empty;

    /// <summary>Fallback do signing secret SaaS (conta plataforma) quando <see cref="StripeThinSaasWebhookSecret"/> está vazio.</summary>
    public string StripeSaasWebhookSecret { get; set; } = string.Empty;

    /// <summary>Signing secret preferido do Event Destination thin (SaaS).</summary>
    public string StripeThinSaasWebhookSecret { get; set; } = string.Empty;

    /// <summary>Informacional: test ou live.</summary>
    public string StripeEnvironment { get; set; } = "test";

    /// <summary>URL base pública (API ou SPA) para success/cancel de Checkout (ex.: https://app.exemplo.com).</summary>
    public string PublicAppBaseUrl { get; set; } = string.Empty;
}
