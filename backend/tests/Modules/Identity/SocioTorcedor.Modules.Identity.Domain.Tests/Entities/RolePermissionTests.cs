using FluentAssertions;
using SocioTorcedor.Modules.Identity.Domain.Entities;

namespace SocioTorcedor.Modules.Identity.Domain.Tests.Entities;

public class RolePermissionTests
{
    [Fact]
    public void Create_sets_ids()
    {
        var permId = Guid.NewGuid();

        var rp = RolePermission.Create("role-1", permId);

        rp.RoleId.Should().Be("role-1");
        rp.PermissionId.Should().Be(permId);
    }

    [Fact]
    public void Create_empty_role_throws()
    {
        var act = () => RolePermission.Create(" ", Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }
}
