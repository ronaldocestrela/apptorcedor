namespace SocioTorcedor.Modules.Payments.Application.Contracts;

/// <summary>
/// Aplica efeitos de domínio para webhooks Stripe da conta do tenant (cobrança de sócios).
/// </summary>
public interface IMemberStripeWebhookEffectApplicator
{
    Task ApplyAsync(string eventType, string rawJson, CancellationToken cancellationToken);
}
