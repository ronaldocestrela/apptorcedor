namespace SocioTorcedor.Modules.Payments.Application.Contracts;

/// <summary>
/// Aplica efeitos de domínio para webhooks SaaS (corpo estilo snapshot ou legado).
/// </summary>
public interface ITenantSaasStripeWebhookEffectApplicator
{
    Task ApplyAsync(string rawBody, CancellationToken cancellationToken);
}
