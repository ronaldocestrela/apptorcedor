using Xunit;

namespace AppTorcedor.Identity.Tests;

public sealed class SystemRolesTests
{
    [Fact]
    public void All_contains_seven_roles_from_agents_spec()
    {
        Assert.Equal(7, SystemRoles.All.Count);
        Assert.Contains(SystemRoles.AdministradorMaster, SystemRoles.All);
        Assert.Contains(SystemRoles.Administrador, SystemRoles.All);
        Assert.Contains(SystemRoles.Financeiro, SystemRoles.All);
        Assert.Contains(SystemRoles.Atendimento, SystemRoles.All);
        Assert.Contains(SystemRoles.Marketing, SystemRoles.All);
        Assert.Contains(SystemRoles.Operador, SystemRoles.All);
        Assert.Contains(SystemRoles.Torcedor, SystemRoles.All);
    }
}
