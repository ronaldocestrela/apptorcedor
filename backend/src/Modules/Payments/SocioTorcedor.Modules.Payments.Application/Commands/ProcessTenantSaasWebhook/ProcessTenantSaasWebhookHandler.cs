using Microsoft.Extensions.Options;
using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.Options;
using SocioTorcedor.Modules.Payments.Domain.Entities;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Application.Commands.ProcessTenantSaasWebhook;

/// <summary>
/// Persiste o webhook em inbox (idempotente) e aplica efeitos (legado + eventos Stripe nativos ou corpo sintético thin).
/// </summary>
public sealed class ProcessTenantSaasWebhookHandler(
    ITenantMasterPaymentsRepository repository,
    ITenantSaasStripeWebhookEffectApplicator effectApplicator,
    IOptions<StripeWebhookHandlingOptions> webhookBehavior)
    : ICommandHandler<ProcessTenantSaasWebhookCommand>
{
    public async Task<Result> Handle(ProcessTenantSaasWebhookCommand command, CancellationToken cancellationToken)
    {
        if (webhookBehavior.Value.StripeWebhookShadowMode)
        {
            await effectApplicator.ApplyAsync(command.RawBody, cancellationToken);
            return Result.Ok();
        }

        var inbox = await repository.GetWebhookByIdempotencyKeyAsync(command.IdempotencyKey, cancellationToken);
        if (inbox is null)
        {
            inbox = TenantPaymentWebhookInbox.Receive(command.IdempotencyKey, command.EventType, command.RawBody);
            await repository.AddWebhookAsync(inbox, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
        }
        else if (inbox.Status == WebhookInboxStatus.Processed)
        {
            return Result.Ok();
        }

        try
        {
            await effectApplicator.ApplyAsync(command.RawBody, cancellationToken);
            inbox.MarkProcessed();
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            inbox.MarkFailed(ex.Message);
            await repository.SaveChangesAsync(cancellationToken);
            return Result.Fail(Error.Failure("Payments.Webhook.ProcessFailed", ex.Message));
        }

        return Result.Ok();
    }
}
