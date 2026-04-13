import { describe, expect, it } from 'vitest'
import { ADMIN_AREA_PERMISSIONS, ApplicationPermissions } from './applicationPermissions'
import { canAccessAdminArea, hasAnyPermission, hasPermission } from './permissionUtils'

describe('permissionUtils', () => {
  const user = {
    permissions: [
      ApplicationPermissions.ConfiguracoesVisualizar,
      ApplicationPermissions.AdministracaoDiagnostics,
    ],
  }

  it('hasPermission', () => {
    expect(hasPermission(user, ApplicationPermissions.ConfiguracoesVisualizar)).toBe(true)
    expect(hasPermission(user, ApplicationPermissions.SociosGerenciar)).toBe(false)
  })

  it('hasAnyPermission', () => {
    expect(hasAnyPermission(user, [ApplicationPermissions.SociosGerenciar, ApplicationPermissions.ConfiguracoesEditar])).toBe(false)
    expect(hasAnyPermission(user, [ApplicationPermissions.SociosGerenciar, ApplicationPermissions.AdministracaoDiagnostics])).toBe(true)
  })

  it('canAccessAdminArea', () => {
    expect(canAccessAdminArea(user, ADMIN_AREA_PERMISSIONS)).toBe(true)
    expect(canAccessAdminArea({ permissions: [] }, ADMIN_AREA_PERMISSIONS)).toBe(false)
    expect(canAccessAdminArea(null, ADMIN_AREA_PERMISSIONS)).toBe(false)
  })

  it('allows admin area with Usuarios.Visualizar alone', () => {
    expect(
      canAccessAdminArea({ permissions: [ApplicationPermissions.UsuariosVisualizar] }, ADMIN_AREA_PERMISSIONS),
    ).toBe(true)
  })

  it('allows admin area with LGPD document view alone', () => {
    expect(
      canAccessAdminArea({ permissions: [ApplicationPermissions.LgpdDocumentosVisualizar] }, ADMIN_AREA_PERMISSIONS),
    ).toBe(true)
  })

  it('allows admin area with Carteirinha.Visualizar alone', () => {
    expect(
      canAccessAdminArea({ permissions: [ApplicationPermissions.CarteirinhaVisualizar] }, ADMIN_AREA_PERMISSIONS),
    ).toBe(true)
  })
})
