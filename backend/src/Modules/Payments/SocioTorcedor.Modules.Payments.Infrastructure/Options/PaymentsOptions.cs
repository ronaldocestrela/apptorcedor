namespace SocioTorcedor.Modules.Payments.Infrastructure.Options;

public sealed class PaymentsOptions
{
    public const string SectionName = "Payments";

    /// <summary>
    /// Segredo compartilhado para webhooks do tenant (header X-Payments-Webhook-Secret).
    /// </summary>
    public string MemberWebhookSecret { get; set; } = string.Empty;
}
