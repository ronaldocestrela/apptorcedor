using Stripe;

namespace SocioTorcedor.Modules.Payments.Application.Contracts;

/// <summary>
/// Busca o recurso atualizado na Stripe a partir de um thin event e monta JSON no formato snapshot.
/// </summary>
public interface IStripeThinWebhookPayloadFactory
{
    Task<StripeThinSyntheticWebhook?> BuildAsync(
        StripeClient stripeClient,
        StripeThinWebhookDispatch dispatch,
        string notificationId,
        string notificationType,
        CancellationToken cancellationToken);
}
