namespace AppTorcedor.Identity;

/// <summary>Granular permission names (separate from roles). JWT carries claims of type <see cref="AppClaimTypes.Permission"/>.</summary>
public static class ApplicationPermissions
{
    public const string AdministracaoDiagnostics = "Administracao.Diagnostics";

    public const string UsuariosVisualizar = "Usuarios.Visualizar";
    public const string UsuariosEditar = "Usuarios.Editar";
    public const string SociosGerenciar = "Socios.Gerenciar";
    public const string PlanosVisualizar = "Planos.Visualizar";
    public const string PlanosCriar = "Planos.Criar";
    public const string PlanosEditar = "Planos.Editar";
    public const string PagamentosVisualizar = "Pagamentos.Visualizar";
    public const string PagamentosGerenciar = "Pagamentos.Gerenciar";
    public const string PagamentosEstornar = "Pagamentos.Estornar";
    public const string JogosCriar = "Jogos.Criar";
    public const string IngressosGerenciar = "Ingressos.Gerenciar";
    public const string NoticiasPublicar = "Noticias.Publicar";
    public const string ChamadosResponder = "Chamados.Responder";
    public const string CarteirinhaVisualizar = "Carteirinha.Visualizar";
    public const string CarteirinhaGerenciar = "Carteirinha.Gerenciar";
    public const string ConfiguracoesVisualizar = "Configuracoes.Visualizar";
    public const string ConfiguracoesEditar = "Configuracoes.Editar";

    public const string LgpdDocumentosVisualizar = "Lgpd.Documentos.Visualizar";
    public const string LgpdDocumentosEditar = "Lgpd.Documentos.Editar";
    public const string LgpdConsentimentosVisualizar = "Lgpd.Consentimentos.Visualizar";
    public const string LgpdConsentimentosRegistrar = "Lgpd.Consentimentos.Registrar";
    public const string LgpdDadosExportar = "Lgpd.Dados.Exportar";
    public const string LgpdDadosAnonimizar = "Lgpd.Dados.Anonimizar";

    /// <summary>All permissions seeded in the database; assign to roles via <c>RolePermissions</c>.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        AdministracaoDiagnostics,
        UsuariosVisualizar,
        UsuariosEditar,
        SociosGerenciar,
        PlanosVisualizar,
        PlanosCriar,
        PlanosEditar,
        PagamentosVisualizar,
        PagamentosGerenciar,
        PagamentosEstornar,
        JogosCriar,
        IngressosGerenciar,
        NoticiasPublicar,
        ChamadosResponder,
        CarteirinhaVisualizar,
        CarteirinhaGerenciar,
        ConfiguracoesVisualizar,
        ConfiguracoesEditar,
        LgpdDocumentosVisualizar,
        LgpdDocumentosEditar,
        LgpdConsentimentosVisualizar,
        LgpdConsentimentosRegistrar,
        LgpdDadosExportar,
        LgpdDadosAnonimizar,
    ];
}
