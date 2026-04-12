using Stripe;

namespace SocioTorcedor.Modules.Payments.Application.Contracts;

/// <summary>
/// Cliente Stripe e segredo de assinatura do webhook para o tenant (conta própria).
/// </summary>
public sealed record MemberStripeWebhookIngress(StripeClient Client, string WebhookSecret);
