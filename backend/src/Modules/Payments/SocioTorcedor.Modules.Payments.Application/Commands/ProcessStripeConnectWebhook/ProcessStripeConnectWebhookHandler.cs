using Microsoft.Extensions.Options;
using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.Options;
using SocioTorcedor.Modules.Payments.Domain.Entities;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Application.Commands.ProcessStripeConnectWebhook;

public sealed class ProcessStripeConnectWebhookHandler(
    ITenantMasterPaymentsRepository masterPaymentsRepository,
    IConnectStripeWebhookEffectApplicator effectApplicator,
    IOptions<StripeWebhookHandlingOptions> webhookBehavior)
    : ICommandHandler<ProcessStripeConnectWebhookCommand>
{
    public async Task<Result> Handle(ProcessStripeConnectWebhookCommand command, CancellationToken cancellationToken)
    {
        if (webhookBehavior.Value.StripeWebhookShadowMode)
        {
            await effectApplicator.ApplyAsync(command.EventType, command.RawJson, cancellationToken);
            return Result.Ok();
        }

        var inbox = await masterPaymentsRepository.GetConnectWebhookByIdempotencyKeyAsync(command.StripeEventId, cancellationToken);
        if (inbox is null)
        {
            inbox = ConnectStripeWebhookInbox.Receive(command.StripeEventId, command.EventType, command.RawJson);
            await masterPaymentsRepository.AddConnectWebhookAsync(inbox, cancellationToken);
            await masterPaymentsRepository.SaveChangesAsync(cancellationToken);
        }
        else if (inbox.Status == WebhookInboxStatus.Processed)
            return Result.Ok();

        try
        {
            await effectApplicator.ApplyAsync(command.EventType, command.RawJson, cancellationToken);
            inbox.MarkProcessed();
            await masterPaymentsRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            inbox.MarkFailed(ex.Message);
            await masterPaymentsRepository.SaveChangesAsync(cancellationToken);
            return Result.Fail(Error.Failure("Payments.ConnectWebhook.ProcessFailed", ex.Message));
        }

        return Result.Ok();
    }
}
