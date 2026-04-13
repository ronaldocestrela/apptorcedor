/**
 * Mirrors backend `AppTorcedor.Identity.ApplicationPermissions` (single source of truth on server).
 * Keep names identical for JWT policies and `/api/auth/me`.
 */
export const ApplicationPermissions = {
  AdministracaoDiagnostics: 'Administracao.Diagnostics',
  UsuariosVisualizar: 'Usuarios.Visualizar',
  UsuariosEditar: 'Usuarios.Editar',
  SociosGerenciar: 'Socios.Gerenciar',
  PlanosCriar: 'Planos.Criar',
  PlanosEditar: 'Planos.Editar',
  PagamentosEstornar: 'Pagamentos.Estornar',
  JogosCriar: 'Jogos.Criar',
  IngressosGerenciar: 'Ingressos.Gerenciar',
  NoticiasPublicar: 'Noticias.Publicar',
  ChamadosResponder: 'Chamados.Responder',
  ConfiguracoesVisualizar: 'Configuracoes.Visualizar',
  ConfiguracoesEditar: 'Configuracoes.Editar',
} as const

export type ApplicationPermission =
  (typeof ApplicationPermissions)[keyof typeof ApplicationPermissions]

/** Any permission that unlocks at least one item in the admin shell. */
export const ADMIN_AREA_PERMISSIONS: readonly ApplicationPermission[] = [
  ApplicationPermissions.AdministracaoDiagnostics,
  ApplicationPermissions.ConfiguracoesVisualizar,
  ApplicationPermissions.ConfiguracoesEditar,
  ApplicationPermissions.SociosGerenciar,
]
