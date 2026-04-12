using System.Text.Json;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.DTOs;
using SocioTorcedor.Modules.Payments.Domain.Enums;
using Stripe;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Services;

/// <summary>
/// Resolve <see cref="StripePaymentOperations"/> para cobrança de sócio usando chaves do tenant (Stripe direto).
/// </summary>
public sealed class MemberStripeOperationsResolver(
    ITenantMasterPaymentsRepository masterPaymentsRepository,
    ITenantMemberGatewayCredentialProtector protector)
{
    public async Task<StripePaymentOperations?> TryResolveAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var cfg = await masterPaymentsRepository.GetMemberGatewayConfigurationByTenantIdAsync(tenantId, cancellationToken);
        if (cfg is null || cfg.SelectedProvider != MemberPaymentProviderKind.StripeDirect)
            return null;

        if (cfg.Status != MemberGatewayConfigurationStatus.Ready || string.IsNullOrWhiteSpace(cfg.ProtectedCredentials))
            return null;

        try
        {
            var json = protector.Unprotect(cfg.ProtectedCredentials);
            var creds = JsonSerializer.Deserialize<StripeDirectCredentialsDto>(json);
            if (string.IsNullOrWhiteSpace(creds?.SecretKey))
                return null;

            return new StripePaymentOperations(new StripeClient(creds.SecretKey.Trim()));
        }
        catch
        {
            return null;
        }
    }
}
