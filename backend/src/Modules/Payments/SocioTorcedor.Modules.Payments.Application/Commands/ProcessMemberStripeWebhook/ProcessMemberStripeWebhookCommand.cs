using SocioTorcedor.BuildingBlocks.Application.Abstractions;

namespace SocioTorcedor.Modules.Payments.Application.Commands.ProcessMemberStripeWebhook;

public sealed record ProcessMemberStripeWebhookCommand(
    string StripeEventId,
    string EventType,
    string RawJson) : ICommand;
