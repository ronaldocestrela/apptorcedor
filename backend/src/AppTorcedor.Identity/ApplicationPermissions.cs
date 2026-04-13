namespace AppTorcedor.Identity;

/// <summary>Granular permission names (separate from roles). JWT carries claims of type <see cref="AppClaimTypes.Permission"/>.</summary>
public static class ApplicationPermissions
{
    public const string AdministracaoDiagnostics = "Administracao.Diagnostics";

    public const string UsuariosVisualizar = "Usuarios.Visualizar";
    public const string UsuariosEditar = "Usuarios.Editar";
    public const string SociosGerenciar = "Socios.Gerenciar";
    public const string PlanosCriar = "Planos.Criar";
    public const string PlanosEditar = "Planos.Editar";
    public const string PagamentosEstornar = "Pagamentos.Estornar";
    public const string JogosCriar = "Jogos.Criar";
    public const string IngressosGerenciar = "Ingressos.Gerenciar";
    public const string NoticiasPublicar = "Noticias.Publicar";
    public const string ChamadosResponder = "Chamados.Responder";
    public const string ConfiguracoesVisualizar = "Configuracoes.Visualizar";
    public const string ConfiguracoesEditar = "Configuracoes.Editar";

    /// <summary>All permissions seeded in the database; assign to roles via <c>RolePermissions</c>.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        AdministracaoDiagnostics,
        UsuariosVisualizar,
        UsuariosEditar,
        SociosGerenciar,
        PlanosCriar,
        PlanosEditar,
        PagamentosEstornar,
        JogosCriar,
        IngressosGerenciar,
        NoticiasPublicar,
        ChamadosResponder,
        ConfiguracoesVisualizar,
        ConfiguracoesEditar,
    ];
}
