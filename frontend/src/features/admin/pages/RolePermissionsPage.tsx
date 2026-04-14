import { useCallback, useEffect, useMemo, useState } from 'react'
import { isAxiosError } from 'axios'
import {
  ALL_APPLICATION_PERMISSION_VALUES,
  ALL_SYSTEM_ROLES,
  ApplicationPermissions,
} from '../../../shared/auth/applicationPermissions'
import { hasPermission } from '../../../shared/auth/permissionUtils'
import { PermissionGate } from '../../auth/PermissionGate'
import { useAuth } from '../../auth/AuthContext'
import { listRolePermissions, replaceRolePermissions, type RolePermissionRow } from '../services/adminApi'

function buildDraft(rows: RolePermissionRow[]): Record<string, string[]> {
  const draft: Record<string, string[]> = {}
  for (const r of ALL_SYSTEM_ROLES)
    draft[r] = []
  for (const row of rows) {
    if (!draft[row.roleName])
      draft[row.roleName] = []
    draft[row.roleName].push(row.permissionName)
  }
  for (const k of Object.keys(draft))
    draft[k] = [...new Set(draft[k])].sort()
  return draft
}

export function RolePermissionsPage() {
  const { user } = useAuth()
  const canEdit = hasPermission(user, ApplicationPermissions.ConfiguracoesEditar)
  const [draft, setDraft] = useState<Record<string, string[]>>({})
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [savingRole, setSavingRole] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await listRolePermissions()
      setDraft(buildDraft(data))
    } catch {
      setError('Falha ao carregar matriz role × permissão.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  const byRole = useMemo(() => ALL_SYSTEM_ROLES.map((role) => [role, draft[role] ?? []] as const), [draft])

  const toggle = (role: string, perm: string) => {
    setDraft((d) => {
      const cur = new Set(d[role] ?? [])
      if (cur.has(perm))
        cur.delete(perm)
      else
        cur.add(perm)
      return { ...d, [role]: [...cur].sort() }
    })
  }

  const saveRole = async (role: string) => {
    if (!canEdit)
      return
    setSavingRole(role)
    setError(null)
    try {
      await replaceRolePermissions(role, draft[role] ?? [])
      await load()
    } catch (err) {
      const msg = isAxiosError(err) && err.response?.data && typeof err.response.data === 'object' && 'message' in err.response.data
        ? String((err.response.data as { message: string }).message)
        : 'Falha ao salvar permissões da role.'
      setError(msg)
    } finally {
      setSavingRole(null)
    }
  }

  return (
    <PermissionGate anyOf={[ApplicationPermissions.ConfiguracoesVisualizar]}>
      <h1>Roles × Permissões</h1>
      <p style={{ color: '#555', maxWidth: 720 }}>
        {canEdit
          ? 'Marque as permissões por role e salve cada bloco. Alterações afetam novos tokens JWT após login.'
          : 'Somente leitura. É necessária a permissão Configuracoes.Editar para alterar a matriz.'}
      </p>
      {error ? <p role="alert" style={{ color: 'crimson' }}>{error}</p> : null}
      {loading ? <p>Carregando...</p> : null}
      {!loading
        ? byRole.map(([role, perms]) => (
            <section key={role} style={{ marginBottom: '2rem', borderBottom: '1px solid #eee', paddingBottom: '1rem' }}>
              <h2 style={{ fontSize: '1.05rem' }}>{role}</h2>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(240px, 1fr))', gap: 6 }}>
                {ALL_APPLICATION_PERMISSION_VALUES.map((p) => (
                  <label key={p} style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 14 }}>
                    <input
                      type="checkbox"
                      checked={perms.includes(p)}
                      disabled={!canEdit}
                      onChange={() => toggle(role, p)}
                    />
                    {p}
                  </label>
                ))}
              </div>
              {canEdit ? (
                <button
                  type="button"
                  style={{ marginTop: 12 }}
                  disabled={savingRole !== null}
                  onClick={() => void saveRole(role)}
                >
                  {savingRole === role ? 'Salvando...' : `Salvar ${role}`}
                </button>
              ) : null}
            </section>
          ))
        : null}
    </PermissionGate>
  )
}
