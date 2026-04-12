using SocioTorcedor.BuildingBlocks.Shared.Results;

namespace SocioTorcedor.Modules.Payments.Application.Contracts;

/// <summary>
/// Valida se o tenant pode cobrar sócios com o provedor configurado (Stripe direto, etc.).
/// </summary>
public interface IMemberPaymentGatewayService
{
    Task<Result> EnsureMemberGatewayReadyForChargeAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
