namespace SocioTorcedor.Modules.Payments.Infrastructure.Options;

public sealed class PaymentsOptions
{
    public const string SectionName = "Payments";

    /// <summary>
    /// Segredo compartilhado para webhooks do tenant (header X-Payments-Webhook-Secret).
    /// </summary>
    public string MemberWebhookSecret { get; set; } = string.Empty;

    /// <summary>Chave secreta da API Stripe (sk_test_... / sk_live_...).</summary>
    public string StripeSecretKey { get; set; } = string.Empty;

    /// <summary>Opcional: publishable key para o frontend (pk_...).</summary>
    public string StripePublishableKey { get; set; } = string.Empty;

    /// <summary>Webhook signing secret para eventos de Billing SaaS (conta da plataforma), formato snapshot V1 (legado).</summary>
    public string StripeSaasWebhookSecret { get; set; } = string.Empty;

    /// <summary>Signing secret do Event Destination thin (SaaS). Se vazio, usa <see cref="StripeSaasWebhookSecret"/>.</summary>
    public string StripeThinSaasWebhookSecret { get; set; } = string.Empty;

    /// <summary>Webhook signing secret para eventos Connect / contas conectadas, formato snapshot V1 (legado).</summary>
    public string StripeConnectWebhookSecret { get; set; } = string.Empty;

    /// <summary>Signing secret do Event Destination thin (Connect). Se vazio, usa <see cref="StripeConnectWebhookSecret"/>.</summary>
    public string StripeThinConnectWebhookSecret { get; set; } = string.Empty;

    /// <summary>Informacional: test ou live.</summary>
    public string StripeEnvironment { get; set; } = "test";

    /// <summary>
    /// URL base pública (API ou SPA) para montar success/cancel de Checkout e links de Connect
    /// (ex.: https://app.exemplo.com ou https://api.exemplo.com).
    /// </summary>
    public string PublicAppBaseUrl { get; set; } = string.Empty;
}
