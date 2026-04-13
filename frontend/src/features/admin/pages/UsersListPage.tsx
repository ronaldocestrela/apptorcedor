import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { isAxiosError } from 'axios'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { hasPermission } from '../../../shared/auth/permissionUtils'
import { PermissionGate } from '../../auth/PermissionGate'
import { useAuth } from '../../auth/AuthContext'
import { listAdminUsers, type AdminUserListItem } from '../services/adminApi'

export function UsersListPage() {
  const { user } = useAuth()
  const canView = hasPermission(user, ApplicationPermissions.UsuariosVisualizar)
  const [items, setItems] = useState<AdminUserListItem[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [searchDebounced, setSearchDebounced] = useState('')
  const [activeFilter, setActiveFilter] = useState<'all' | 'true' | 'false'>('all')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const t = window.setTimeout(() => setSearchDebounced(search.trim()), 350)
    return () => window.clearTimeout(t)
  }, [search])

  const reload = useCallback(async () => {
    if (!canView)
      return
    setLoading(true)
    setError(null)
    try {
      const isActive = activeFilter === 'all' ? undefined : activeFilter === 'true'
      const res = await listAdminUsers({
        search: searchDebounced || undefined,
        isActive,
        page,
        pageSize: 20,
      })
      setItems(res.items)
      setTotalCount(res.totalCount)
    } catch (err: unknown) {
      const msg = isAxiosError(err) ? 'Falha ao carregar usuários.' : 'Erro inesperado.'
      setError(msg)
    } finally {
      setLoading(false)
    }
  }, [canView, searchDebounced, activeFilter, page])

  useEffect(() => {
    void reload()
  }, [reload])

  return (
    <PermissionGate anyOf={[ApplicationPermissions.UsuariosVisualizar]}>
      <section>
        <h1 style={{ marginTop: 0 }}>Usuários</h1>
        <p style={{ color: '#555', maxWidth: 720 }}>
          Contas do sistema (torcedores, não associados e staff). Para convites e papéis internos, use
          {' '}
          <Link to="/admin/staff">Staff</Link>.
        </p>

        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12, marginBottom: 16, alignItems: 'center' }}>
          <label style={{ display: 'flex', flexDirection: 'column', fontSize: 13 }}>
            Busca (nome, e-mail, documento)
            <input
              type="search"
              value={search}
              onChange={(e) => { setPage(1); setSearch(e.target.value) }}
              placeholder="Ex.: nome ou CPF"
              style={{ minWidth: 240, padding: '6px 8px', marginTop: 4 }}
            />
          </label>
          <label style={{ display: 'flex', flexDirection: 'column', fontSize: 13 }}>
            Conta ativa
            <select
              value={activeFilter}
              onChange={(e) => { setPage(1); setActiveFilter(e.target.value as typeof activeFilter) }}
              style={{ marginTop: 4, padding: '6px 8px' }}
            >
              <option value="all">Todas</option>
              <option value="true">Somente ativas</option>
              <option value="false">Somente inativas</option>
            </select>
          </label>
        </div>

        {loading ? <p>Carregando...</p> : null}
        {error ? <p style={{ color: '#b00020' }}>{error}</p> : null}

        {!loading && !error ? (
          <>
            <p style={{ fontSize: 14, color: '#444' }}>
              Total:
              {' '}
              {totalCount}
              {' '}
              — página
              {' '}
              {page}
            </p>
            <div style={{ overflowX: 'auto' }}>
              <table style={{ borderCollapse: 'collapse', width: '100%', fontSize: 14 }}>
                <thead>
                  <tr style={{ textAlign: 'left', borderBottom: '1px solid #ccc' }}>
                    <th style={{ padding: 8 }}>Nome</th>
                    <th style={{ padding: 8 }}>E-mail</th>
                    <th style={{ padding: 8 }}>Documento</th>
                    <th style={{ padding: 8 }}>Associação</th>
                    <th style={{ padding: 8 }}>Staff</th>
                    <th style={{ padding: 8 }}>Ativo</th>
                    <th style={{ padding: 8 }} />
                  </tr>
                </thead>
                <tbody>
                  {items.map((row) => (
                    <tr key={row.id} style={{ borderBottom: '1px solid #eee' }}>
                      <td style={{ padding: 8 }}>{row.name}</td>
                      <td style={{ padding: 8 }}>{row.email}</td>
                      <td style={{ padding: 8 }}>{row.document ?? '—'}</td>
                      <td style={{ padding: 8 }}>{row.membershipStatus ?? '—'}</td>
                      <td style={{ padding: 8 }}>{row.isStaff ? 'Sim' : 'Não'}</td>
                      <td style={{ padding: 8 }}>{row.isActive ? 'Sim' : 'Não'}</td>
                      <td style={{ padding: 8 }}>
                        <Link to={`/admin/users/${row.id}`}>Detalhe</Link>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div style={{ marginTop: 16, display: 'flex', gap: 8 }}>
              <button
                type="button"
                disabled={page <= 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
              >
                Anterior
              </button>
              <button
                type="button"
                disabled={page * 20 >= totalCount}
                onClick={() => setPage((p) => p + 1)}
              >
                Próxima
              </button>
            </div>
          </>
        ) : null}
      </section>
    </PermissionGate>
  )
}
