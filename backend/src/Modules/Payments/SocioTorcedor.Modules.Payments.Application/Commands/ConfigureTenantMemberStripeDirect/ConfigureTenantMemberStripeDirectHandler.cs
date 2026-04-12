using System.Text.Json;
using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.DTOs;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Application.Commands.ConfigureTenantMemberStripeDirect;

public sealed class ConfigureTenantMemberStripeDirectHandler(
    ITenantMasterPaymentsRepository masterPaymentsRepository,
    ITenantMemberGatewayCredentialProtector protector)
    : ICommandHandler<ConfigureTenantMemberStripeDirectCommand>
{
    public async Task<Result> Handle(ConfigureTenantMemberStripeDirectCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.SecretKey))
            return Result.Fail(Error.Failure("Payments.Validation", "Secret key is required."));

        var cfg = await masterPaymentsRepository.GetMemberGatewayConfigurationByTenantIdAsync(command.TenantId, cancellationToken);
        if (cfg is null || cfg.SelectedProvider != MemberPaymentProviderKind.StripeDirect)
        {
            return Result.Fail(Error.Failure(
                "Payments.MemberGateway.WrongProvider",
                "Stripe Direct must be selected for this club in the platform backoffice before configuring keys."));
        }

        var creds = new StripeDirectCredentialsDto
        {
            SecretKey = command.SecretKey.Trim(),
            PublishableKey = command.PublishableKey?.Trim(),
            WebhookSecret = command.WebhookSecret?.Trim()
        };

        var json = JsonSerializer.Serialize(creds);
        var protectedPayload = protector.Protect(json);
        cfg.SetProtectedCredentials(protectedPayload, MemberGatewayConfigurationStatus.Ready);
        await masterPaymentsRepository.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
