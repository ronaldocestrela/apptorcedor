namespace AppTorcedor.Api.Authorization;

public static class Policies
{
    public const string PermissionPrefix = "Permission:";

    /// <summary>Dashboard: <see cref="Identity.ApplicationPermissions.UsuariosVisualizar"/> or <see cref="Identity.ApplicationPermissions.ConfiguracoesVisualizar"/>.</summary>
    public const string AdminDashboard = "AdminDashboard";
}
