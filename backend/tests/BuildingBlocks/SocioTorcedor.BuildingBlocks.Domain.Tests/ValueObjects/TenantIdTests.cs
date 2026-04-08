using FluentAssertions;
using SocioTorcedor.BuildingBlocks.Domain.ValueObjects;

namespace SocioTorcedor.BuildingBlocks.Domain.Tests.ValueObjects;

public class TenantIdTests
{
    [Fact]
    public void Create_with_empty_guid_throws()
    {
        var act = () => TenantId.Create(Guid.Empty);

        act.Should().Throw<ArgumentException>().WithMessage("*empty*");
    }

    [Fact]
    public void Create_with_valid_guid_succeeds()
    {
        var g = Guid.NewGuid();

        var id = TenantId.Create(g);

        id.Value.Should().Be(g);
    }

    [Fact]
    public void New_generates_non_empty()
    {
        var id = TenantId.New();

        id.Value.Should().NotBeEmpty();
    }
}
