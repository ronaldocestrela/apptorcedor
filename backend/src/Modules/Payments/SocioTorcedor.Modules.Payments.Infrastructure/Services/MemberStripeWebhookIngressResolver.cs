using System.Text.Json;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.DTOs;
using SocioTorcedor.Modules.Payments.Domain.Enums;
using Stripe;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Services;

public sealed class MemberStripeWebhookIngressResolver(
    ITenantMasterPaymentsRepository masterPaymentsRepository,
    ITenantMemberGatewayCredentialProtector protector)
    : IMemberStripeWebhookIngressResolver
{
    public async Task<Result<MemberStripeWebhookIngress>> ResolveAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var cfg = await masterPaymentsRepository.GetMemberGatewayConfigurationByTenantIdAsync(tenantId, cancellationToken);
        if (cfg is null || cfg.SelectedProvider != MemberPaymentProviderKind.StripeDirect)
        {
            return Result<MemberStripeWebhookIngress>.Fail(Error.Failure(
                "Payments.MemberGateway.NotConfigured",
                "Stripe direct gateway is not selected for this tenant."));
        }

        if (cfg.Status != MemberGatewayConfigurationStatus.Ready || string.IsNullOrWhiteSpace(cfg.ProtectedCredentials))
        {
            return Result<MemberStripeWebhookIngress>.Fail(Error.Failure(
                "Payments.MemberGateway.NotReady",
                "Gateway credentials are not configured for this tenant."));
        }

        StripeDirectCredentialsDto creds;
        try
        {
            var json = protector.Unprotect(cfg.ProtectedCredentials);
            creds = JsonSerializer.Deserialize<StripeDirectCredentialsDto>(json) ?? new StripeDirectCredentialsDto();
        }
        catch
        {
            return Result<MemberStripeWebhookIngress>.Fail(Error.Failure(
                "Payments.MemberGateway.InvalidCredentials",
                "Could not read gateway credentials."));
        }

        if (string.IsNullOrWhiteSpace(creds.SecretKey) || string.IsNullOrWhiteSpace(creds.WebhookSecret))
        {
            return Result<MemberStripeWebhookIngress>.Fail(Error.Failure(
                "Payments.MemberWebhook.SecretMissing",
                "Stripe secret key and webhook signing secret must be configured for member webhooks."));
        }

        var client = new StripeClient(creds.SecretKey.Trim());
        return Result<MemberStripeWebhookIngress>.Ok(new MemberStripeWebhookIngress(client, creds.WebhookSecret.Trim()));
    }
}
