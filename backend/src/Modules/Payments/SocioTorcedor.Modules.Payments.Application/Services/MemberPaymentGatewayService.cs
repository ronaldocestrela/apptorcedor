using System.Text.Json;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.DTOs;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Application.Services;

public sealed class MemberPaymentGatewayService(
    ITenantMasterPaymentsRepository masterPaymentsRepository,
    ITenantMemberGatewayCredentialProtector protector) : IMemberPaymentGatewayService
{
    public async Task<Result> EnsureMemberGatewayReadyForChargeAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var cfg = await masterPaymentsRepository.GetMemberGatewayConfigurationByTenantIdAsync(tenantId, cancellationToken);
        if (cfg is null || cfg.SelectedProvider == MemberPaymentProviderKind.None)
        {
            return Result.Fail(Error.Failure(
                "Payments.MemberGateway.NotSelected",
                "No payment gateway was selected for this club. The platform operator must assign one in the backoffice."));
        }

        if (cfg.SelectedProvider == MemberPaymentProviderKind.Asaas || cfg.SelectedProvider == MemberPaymentProviderKind.MercadoPago)
        {
            return Result.Fail(Error.Failure(
                "Payments.MemberGateway.NotImplemented",
                "This payment provider is not implemented yet."));
        }

        if (cfg.SelectedProvider != MemberPaymentProviderKind.StripeDirect)
        {
            return Result.Fail(Error.Failure("Payments.MemberGateway.Unsupported", "Unsupported payment provider."));
        }

        if (cfg.Status != MemberGatewayConfigurationStatus.Ready || string.IsNullOrWhiteSpace(cfg.ProtectedCredentials))
        {
            return Result.Fail(Error.Failure(
                "Payments.MemberGateway.NotReady",
                "Payment gateway credentials are not configured or validated for this club."));
        }

        try
        {
            var json = protector.Unprotect(cfg.ProtectedCredentials);
            var creds = JsonSerializer.Deserialize<StripeDirectCredentialsDto>(json);
            if (string.IsNullOrWhiteSpace(creds?.SecretKey))
            {
                return Result.Fail(Error.Failure(
                    "Payments.MemberGateway.NotReady",
                    "Stripe secret key is missing for this club."));
            }
        }
        catch
        {
            return Result.Fail(Error.Failure(
                "Payments.MemberGateway.InvalidCredentials",
                "Could not read gateway credentials for this club."));
        }

        return Result.Ok();
    }
}
