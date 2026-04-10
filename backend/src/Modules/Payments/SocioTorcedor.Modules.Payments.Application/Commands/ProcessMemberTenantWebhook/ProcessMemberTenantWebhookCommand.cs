using SocioTorcedor.BuildingBlocks.Application.Abstractions;

namespace SocioTorcedor.Modules.Payments.Application.Commands.ProcessMemberTenantWebhook;

public sealed record ProcessMemberTenantWebhookCommand(
    string IdempotencyKey,
    string EventType,
    string RawBody) : ICommand;
