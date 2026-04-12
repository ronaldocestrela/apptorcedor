namespace SocioTorcedor.Modules.Payments.Application.DTOs;

/// <summary>Segredos Stripe do tenant (conta própria, sem Connect).</summary>
public sealed class StripeDirectCredentialsDto
{
    public string? SecretKey { get; set; }

    public string? PublishableKey { get; set; }

    /// <summary>Signing secret do webhook configurado na conta Stripe do tenant (opcional).</summary>
    public string? WebhookSecret { get; set; }
}
