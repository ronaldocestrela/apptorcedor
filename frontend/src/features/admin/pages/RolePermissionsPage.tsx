import { useEffect, useMemo, useState } from 'react'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { PermissionGate } from '../../auth/PermissionGate'
import { listRolePermissions, type RolePermissionRow } from '../services/adminApi'

export function RolePermissionsPage() {
  const [rows, setRows] = useState<RolePermissionRow[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const run = async () => {
      setLoading(true)
      setError(null)
      try {
        setRows(await listRolePermissions())
      } catch {
        setError('Falha ao carregar matriz role × permissão.')
      } finally {
        setLoading(false)
      }
    }
    void run()
  }, [])

  const byRole = useMemo(() => {
    const m = new Map<string, string[]>()
    for (const r of rows) {
      const list = m.get(r.roleName) ?? []
      list.push(r.permissionName)
      m.set(r.roleName, list)
    }
    for (const [, perms] of m)
      perms.sort()
    return [...m.entries()].sort((a, b) => a[0].localeCompare(b[0]))
  }, [rows])

  return (
    <PermissionGate anyOf={[ApplicationPermissions.ConfiguracoesVisualizar]}>
      <h1>Roles × Permissões</h1>
      <p style={{ color: '#555', maxWidth: 640 }}>Somente leitura. Edição virá com a evolução do backoffice.</p>
      {error ? <p role="alert" style={{ color: 'crimson' }}>{error}</p> : null}
      {loading ? <p>Carregando...</p> : null}
      {!loading && byRole.map(([role, perms]) => (
        <section key={role} style={{ marginBottom: '1.5rem' }}>
          <h2 style={{ fontSize: '1.05rem' }}>{role}</h2>
          <ul style={{ margin: 0 }}>
            {perms.map((p) => (
              <li key={p}>{p}</li>
            ))}
          </ul>
        </section>
      ))}
    </PermissionGate>
  )
}
