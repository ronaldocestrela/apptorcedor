using SocioTorcedor.BuildingBlocks.Application.Abstractions;

namespace SocioTorcedor.Modules.Payments.Application.Commands.ProcessStripeConnectWebhook;

public sealed record ProcessStripeConnectWebhookCommand(
    string StripeEventId,
    string EventType,
    string RawJson) : ICommand;
