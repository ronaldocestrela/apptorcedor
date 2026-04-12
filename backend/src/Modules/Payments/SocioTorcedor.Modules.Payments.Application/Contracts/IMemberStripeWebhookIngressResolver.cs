using SocioTorcedor.BuildingBlocks.Shared.Results;

namespace SocioTorcedor.Modules.Payments.Application.Contracts;

public interface IMemberStripeWebhookIngressResolver
{
    Task<Result<MemberStripeWebhookIngress>> ResolveAsync(Guid tenantId, CancellationToken cancellationToken);
}
