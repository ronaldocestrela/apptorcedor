using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Commands.ReplaceRolePermissions;
using AppTorcedor.Identity;

namespace AppTorcedor.Application.Tests;

public sealed class ReplaceRolePermissionsCommandHandlerTests
{
    [Fact]
    public async Task Throws_when_clearing_all_permissions_from_administrador_master()
    {
        var fake = new FakeRolePermissionWritePort();
        var handler = new ReplaceRolePermissionsCommandHandler(fake);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(
                new ReplaceRolePermissionsCommand(SystemRoles.AdministradorMaster, []),
                CancellationToken.None));
        Assert.Empty(fake.Calls);
    }

    [Fact]
    public async Task Delegates_to_port_for_non_master_roles()
    {
        var fake = new FakeRolePermissionWritePort();
        var handler = new ReplaceRolePermissionsCommandHandler(fake);
        await handler.Handle(
            new ReplaceRolePermissionsCommand(SystemRoles.Administrador, [ApplicationPermissions.UsuariosVisualizar]),
            CancellationToken.None);
        Assert.Single(fake.Calls);
        Assert.Equal(SystemRoles.Administrador, fake.Calls[0].Role);
        Assert.Equal(ApplicationPermissions.UsuariosVisualizar, fake.Calls[0].Permissions[0]);
    }

    private sealed class FakeRolePermissionWritePort : IRolePermissionWritePort
    {
        public List<(string Role, IReadOnlyList<string> Permissions)> Calls { get; } = [];

        public Task ReplaceRolePermissionsAsync(
            string roleName,
            IReadOnlyList<string> permissionNames,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((roleName, permissionNames));
            return Task.CompletedTask;
        }
    }
}
