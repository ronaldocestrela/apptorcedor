namespace SocioTorcedor.Modules.Payments.Application.Contracts;

/// <summary>
/// Aplica efeitos de domínio para webhooks SaaS (payload estilo snapshot Stripe / thin).
/// </summary>
public interface ITenantSaasStripeWebhookEffectApplicator
{
    Task ApplyAsync(string rawBody, CancellationToken cancellationToken);
}
