namespace AppTorcedor.Infrastructure.Options;

/// <summary>Payment gateway configuration (mock vs Stripe, legacy callback secret, Stripe keys/URLs).</summary>
public sealed class PaymentsOptions
{
    public const string SectionName = "Payments";

    /// <summary>Mock (default) or Stripe.</summary>
    public string Provider { get; set; } = "Mock";

    /// <summary>Shared secret for <c>POST /api/subscriptions/payments/callback</c> (testing / legacy integrations).</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    public PaymentsStripeOptions Stripe { get; set; } = new();

    public PaymentsAsaasOptions Asaas { get; set; } = new();
}

/// <summary>ASAAS (cartão parcelado via link de pagamento, PIX, webhooks).</summary>
public sealed class PaymentsAsaasOptions
{
    /// <summary>Chave de API (access token do painel ASAAS).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Token de autenticação do webhook (header <c>asaas-access-token</c>).</summary>
    public string WebhookToken { get; set; } = string.Empty;

    /// <summary>URL de sucesso após pagamento no link (deve estar no domínio cadastrado no ASAAS).</summary>
    public string SuccessUrl { get; set; } = string.Empty;

    /// <summary>URL quando o usuário abandona (opcional no ASAAS; usada em mensagens de erro).</summary>
    public string CancelUrl { get; set; } = string.Empty;

    /// <summary>Base da API: produção <c>https://api.asaas.com</c>, sandbox <c>https://api-sandbox.asaas.com</c>.</summary>
    public string BaseUrl { get; set; } = "https://api.asaas.com";
}

/// <summary>Stripe Checkout + webhooks (cartão).</summary>
public sealed class PaymentsStripeOptions
{
    /// <summary>API secret key (sk_live_… / sk_test_…).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Signing secret for the Stripe webhook endpoint (whsec_…).</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>Success redirect after Checkout (HTTPS in production).</summary>
    public string SuccessUrl { get; set; } = string.Empty;

    /// <summary>Cancel redirect when the user abandons Checkout.</summary>
    public string CancelUrl { get; set; } = string.Empty;
}
