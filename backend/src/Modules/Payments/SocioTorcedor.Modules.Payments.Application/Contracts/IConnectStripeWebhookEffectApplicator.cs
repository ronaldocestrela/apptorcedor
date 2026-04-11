namespace SocioTorcedor.Modules.Payments.Application.Contracts;

/// <summary>
/// Aplica efeitos de domínio para webhooks Stripe Connect (corpo estilo snapshot).
/// </summary>
public interface IConnectStripeWebhookEffectApplicator
{
    Task ApplyAsync(string eventType, string rawJson, CancellationToken cancellationToken);
}
