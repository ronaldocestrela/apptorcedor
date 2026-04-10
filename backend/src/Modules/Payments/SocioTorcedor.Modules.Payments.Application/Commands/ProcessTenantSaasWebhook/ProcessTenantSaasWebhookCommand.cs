using SocioTorcedor.BuildingBlocks.Application.Abstractions;

namespace SocioTorcedor.Modules.Payments.Application.Commands.ProcessTenantSaasWebhook;

public sealed record ProcessTenantSaasWebhookCommand(
    string IdempotencyKey,
    string EventType,
    string RawBody) : ICommand;
