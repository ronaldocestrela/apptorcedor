using SocioTorcedor.BuildingBlocks.Domain.Abstractions;

namespace SocioTorcedor.Modules.Membership.Domain.Events;

public sealed record MemberProfileCreatedDomainEvent(Guid MemberProfileId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
