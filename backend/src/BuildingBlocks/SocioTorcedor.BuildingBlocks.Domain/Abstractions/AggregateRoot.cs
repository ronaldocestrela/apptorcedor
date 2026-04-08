namespace SocioTorcedor.BuildingBlocks.Domain.Abstractions;

public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected AggregateRoot()
    {
    }

    protected AggregateRoot(Guid id)
        : base(id)
    {
    }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
