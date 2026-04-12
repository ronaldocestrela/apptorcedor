using System.Text.Json;
using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.DTOs;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Application.Queries.GetTenantMemberGatewayStatus;

public sealed class GetTenantMemberGatewayStatusHandler(
    ITenantMasterPaymentsRepository masterPaymentsRepository,
    ITenantMemberGatewayCredentialProtector protector)
    : IQueryHandler<GetTenantMemberGatewayStatusQuery, MemberGatewayStatusDto>
{
    public async Task<Result<MemberGatewayStatusDto>> Handle(
        GetTenantMemberGatewayStatusQuery query,
        CancellationToken cancellationToken)
    {
        var cfg = await masterPaymentsRepository.GetMemberGatewayConfigurationByTenantIdAsync(query.TenantId, cancellationToken);
        if (cfg is null)
        {
            return Result<MemberGatewayStatusDto>.Ok(new MemberGatewayStatusDto(
                MemberPaymentProviderKind.None.ToString(),
                MemberGatewayConfigurationStatus.NotConfigured.ToString(),
                null,
                false));
        }

        string? pkHint = null;
        var webhookOk = false;
        if (!string.IsNullOrWhiteSpace(cfg.ProtectedCredentials) && cfg.SelectedProvider == MemberPaymentProviderKind.StripeDirect)
        {
            try
            {
                var json = protector.Unprotect(cfg.ProtectedCredentials);
                var creds = JsonSerializer.Deserialize<StripeDirectCredentialsDto>(json);
                if (!string.IsNullOrWhiteSpace(creds?.PublishableKey))
                {
                    var pk = creds.PublishableKey!;
                    pkHint = pk.Length <= 12 ? pk : $"{pk[..8]}…";
                }

                webhookOk = !string.IsNullOrWhiteSpace(creds?.WebhookSecret);
            }
            catch
            {
                // leave hints empty
            }
        }

        return Result<MemberGatewayStatusDto>.Ok(new MemberGatewayStatusDto(
            cfg.SelectedProvider.ToString(),
            cfg.Status.ToString(),
            pkHint,
            webhookOk));
    }
}
