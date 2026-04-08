using FluentAssertions;
using SocioTorcedor.BuildingBlocks.Domain.Abstractions;

namespace SocioTorcedor.BuildingBlocks.Domain.Tests.Abstractions;

public class AggregateRootTests
{
    private sealed class SampleEvent : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }

    private sealed class SampleAggregate : AggregateRoot
    {
    }

    [Fact]
    public void AddDomainEvent_adds_event()
    {
        var aggregate = new SampleAggregate();
        var evt = new SampleEvent();

        aggregate.AddDomainEvent(evt);

        aggregate.DomainEvents.Should().ContainSingle().Which.Should().Be(evt);
    }

    [Fact]
    public void ClearDomainEvents_removes_all()
    {
        var aggregate = new SampleAggregate();
        aggregate.AddDomainEvent(new SampleEvent());
        aggregate.AddDomainEvent(new SampleEvent());

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.Should().BeEmpty();
    }
}
