namespace AppTorcedor.Api.Authorization;

public static class Policies
{
    public const string PermissionPrefix = "Permission:";

    /// <summary>Dashboard: <see cref="Identity.ApplicationPermissions.UsuariosVisualizar"/> or <see cref="Identity.ApplicationPermissions.ConfiguracoesVisualizar"/>.</summary>
    public const string AdminDashboard = "AdminDashboard";

    /// <summary>Upload opponent logos: <see cref="Identity.ApplicationPermissions.JogosCriar"/> or <see cref="Identity.ApplicationPermissions.JogosEditar"/>.</summary>
    public const string GamesOpponentLogosUpload = "GamesOpponentLogosUpload";

    /// <summary>Partner keys read access: <see cref="Identity.ApplicationPermissions.WebhooksVisualizar"/> or <see cref="Identity.ApplicationPermissions.WebhooksGerenciar"/>.</summary>
    public const string WebhooksRead = "WebhooksRead";
}
