namespace SocioTorcedor.BuildingBlocks.Domain.Abstractions;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
