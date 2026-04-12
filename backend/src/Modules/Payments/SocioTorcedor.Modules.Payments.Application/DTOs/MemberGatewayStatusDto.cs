namespace SocioTorcedor.Modules.Payments.Application.DTOs;

public sealed record MemberGatewayStatusDto(
    string SelectedProvider,
    string Status,
    string? PublishableKeyHint,
    bool WebhookSecretConfigured);
