using SocioTorcedor.BuildingBlocks.Application.Abstractions;

namespace SocioTorcedor.Modules.Payments.Application.Commands.ConfigureTenantMemberStripeDirect;

public sealed record ConfigureTenantMemberStripeDirectCommand(
    Guid TenantId,
    string SecretKey,
    string? PublishableKey,
    string? WebhookSecret) : ICommand;
