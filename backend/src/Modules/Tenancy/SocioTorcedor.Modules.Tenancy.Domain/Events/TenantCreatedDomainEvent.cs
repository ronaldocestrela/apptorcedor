using SocioTorcedor.BuildingBlocks.Domain.Abstractions;

namespace SocioTorcedor.Modules.Tenancy.Domain.Events;

public sealed record TenantCreatedDomainEvent(Guid TenantId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
