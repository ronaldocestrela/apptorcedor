using FluentAssertions;
using SocioTorcedor.Modules.Identity.Domain.Entities;

namespace SocioTorcedor.Modules.Identity.Domain.Tests.Entities;

public class PermissionTests
{
    [Fact]
    public void Create_valid_name_succeeds()
    {
        var p = Permission.Create("Socios.Criar", "Create members");

        p.Name.Should().Be("Socios.Criar");
        p.Description.Should().Be("Create members");
    }

    [Fact]
    public void Create_empty_name_throws()
    {
        var act = () => Permission.Create(" ", "x");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_name_with_space_throws()
    {
        var act = () => Permission.Create("bad name", "x");

        act.Should().Throw<ArgumentException>();
    }
}
