/**
 * Mirrors backend `AppTorcedor.Identity.ApplicationPermissions` (single source of truth on server).
 * Keep names identical for JWT policies and `/api/auth/me`.
 */
export const ApplicationPermissions = {
  AdministracaoDiagnostics: 'Administracao.Diagnostics',
  UsuariosVisualizar: 'Usuarios.Visualizar',
  UsuariosEditar: 'Usuarios.Editar',
  SociosGerenciar: 'Socios.Gerenciar',
  PlanosVisualizar: 'Planos.Visualizar',
  PlanosCriar: 'Planos.Criar',
  PlanosEditar: 'Planos.Editar',
  PagamentosVisualizar: 'Pagamentos.Visualizar',
  PagamentosGerenciar: 'Pagamentos.Gerenciar',
  PagamentosEstornar: 'Pagamentos.Estornar',
  JogosVisualizar: 'Jogos.Visualizar',
  JogosCriar: 'Jogos.Criar',
  JogosEditar: 'Jogos.Editar',
  IngressosVisualizar: 'Ingressos.Visualizar',
  IngressosGerenciar: 'Ingressos.Gerenciar',
  NoticiasPublicar: 'Noticias.Publicar',
  ChamadosResponder: 'Chamados.Responder',
  CarteirinhaVisualizar: 'Carteirinha.Visualizar',
  CarteirinhaGerenciar: 'Carteirinha.Gerenciar',
  ConfiguracoesVisualizar: 'Configuracoes.Visualizar',
  ConfiguracoesEditar: 'Configuracoes.Editar',
  LgpdDocumentosVisualizar: 'Lgpd.Documentos.Visualizar',
  LgpdDocumentosEditar: 'Lgpd.Documentos.Editar',
  LgpdConsentimentosVisualizar: 'Lgpd.Consentimentos.Visualizar',
  LgpdConsentimentosRegistrar: 'Lgpd.Consentimentos.Registrar',
  LgpdDadosExportar: 'Lgpd.Dados.Exportar',
  LgpdDadosAnonimizar: 'Lgpd.Dados.Anonimizar',
} as const

export type ApplicationPermission =
  (typeof ApplicationPermissions)[keyof typeof ApplicationPermissions]

/** Any permission that unlocks at least one item in the admin shell. */
export const ADMIN_AREA_PERMISSIONS: readonly ApplicationPermission[] = [
  ApplicationPermissions.AdministracaoDiagnostics,
  ApplicationPermissions.ConfiguracoesVisualizar,
  ApplicationPermissions.ConfiguracoesEditar,
  ApplicationPermissions.SociosGerenciar,
  ApplicationPermissions.PlanosVisualizar,
  ApplicationPermissions.PlanosCriar,
  ApplicationPermissions.PlanosEditar,
  ApplicationPermissions.UsuariosVisualizar,
  ApplicationPermissions.UsuariosEditar,
  ApplicationPermissions.LgpdDocumentosVisualizar,
  ApplicationPermissions.LgpdDocumentosEditar,
  ApplicationPermissions.LgpdConsentimentosVisualizar,
  ApplicationPermissions.LgpdConsentimentosRegistrar,
  ApplicationPermissions.LgpdDadosExportar,
  ApplicationPermissions.LgpdDadosAnonimizar,
  ApplicationPermissions.PagamentosVisualizar,
  ApplicationPermissions.PagamentosGerenciar,
  ApplicationPermissions.PagamentosEstornar,
  ApplicationPermissions.CarteirinhaVisualizar,
  ApplicationPermissions.CarteirinhaGerenciar,
  ApplicationPermissions.JogosVisualizar,
  ApplicationPermissions.JogosCriar,
  ApplicationPermissions.JogosEditar,
  ApplicationPermissions.IngressosVisualizar,
  ApplicationPermissions.IngressosGerenciar,
  ApplicationPermissions.NoticiasPublicar,
]

/** Mirrors backend <c>SystemRoles.All</c> for matrix UI. */
export const ALL_SYSTEM_ROLES = [
  'Administrador Master',
  'Administrador',
  'Financeiro',
  'Atendimento',
  'Marketing',
  'Operador',
  'Torcedor',
] as const

/** Roles allowed for staff invites (excludes torcedor). */
export const STAFF_ASSIGNABLE_ROLES = ALL_SYSTEM_ROLES.filter((r) => r !== 'Torcedor') as readonly string[]

export const ALL_APPLICATION_PERMISSION_VALUES: readonly ApplicationPermission[] = Object.freeze(
  Object.values(ApplicationPermissions) as ApplicationPermission[],
)
