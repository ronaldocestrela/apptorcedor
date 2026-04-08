using FluentAssertions;
using SocioTorcedor.BuildingBlocks.Domain.Abstractions;

namespace SocioTorcedor.BuildingBlocks.Domain.Tests.Abstractions;

public class EntityTests
{
    private sealed class SampleEntity : Entity
    {
        public SampleEntity()
        {
        }

        public SampleEntity(Guid id)
            : base(id)
        {
        }
    }

    [Fact]
    public void Constructor_without_id_generates_non_empty_guid()
    {
        var entity = new SampleEntity();

        entity.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_with_id_sets_id()
    {
        var id = Guid.NewGuid();

        var entity = new SampleEntity(id);

        entity.Id.Should().Be(id);
    }

    [Fact]
    public void Equals_same_id_same_type_returns_true()
    {
        var id = Guid.NewGuid();
        var a = new SampleEntity(id);
        var b = new SampleEntity(id);

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeFalse(); // reference equality operator not overridden
    }
}
