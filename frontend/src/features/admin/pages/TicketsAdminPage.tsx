import { useCallback, useEffect, useState } from 'react'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import { hasPermission } from '../../../shared/auth/permissionUtils'
import { useAuth } from '../../auth/AuthContext'
import { PermissionGate } from '../../auth/PermissionGate'
import {
  getAdminTicket,
  listAdminGames,
  listAdminTickets,
  purchaseAdminTicket,
  redeemAdminTicket,
  reserveAdminTicket,
  syncAdminTicket,
  type AdminGameListItem,
  type AdminTicketListItem,
} from '../services/adminApi'

export function TicketsAdminPage() {
  const { user } = useAuth()
  const canView = hasPermission(user, ApplicationPermissions.IngressosVisualizar)
  const canManage = hasPermission(user, ApplicationPermissions.IngressosGerenciar)

  const [games, setGames] = useState<AdminGameListItem[]>([])
  const [items, setItems] = useState<AdminTicketListItem[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const [userId, setUserId] = useState('')
  const [gameId, setGameId] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [detailQr, setDetailQr] = useState<string | null>(null)

  const loadGames = useCallback(async () => {
    try {
      const page = await listAdminGames({ isActive: true, pageSize: 200 })
      setGames(page.items)
      setGameId((prev) => prev || (page.items[0]?.gameId ?? ''))
    } catch {
      setError('Falha ao listar jogos.')
    }
  }, [])

  const loadTickets = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const page = await listAdminTickets({
        userId: userId.trim() || undefined,
        gameId: gameId.trim() || undefined,
        status: statusFilter.trim() || undefined,
        pageSize: 100,
      })
      setItems(page.items)
      setTotalCount(page.totalCount)
    } catch {
      setError('Falha ao listar ingressos.')
    } finally {
      setLoading(false)
    }
  }, [userId, gameId, statusFilter])

  useEffect(() => {
    void loadGames()
  }, [loadGames])

  useEffect(() => {
    void loadTickets()
  }, [loadTickets])

  async function onReserve() {
    if (!canManage || !userId.trim() || !gameId.trim())
      return
    setBusy(true)
    setError(null)
    try {
      await reserveAdminTicket({ userId: userId.trim(), gameId: gameId.trim() })
      await loadTickets()
    } catch {
      setError('Falha ao reservar ingresso.')
    } finally {
      setBusy(false)
    }
  }

  async function refreshDetail(id: string) {
    try {
      const d = await getAdminTicket(id)
      setDetailQr(d.qrCode)
    } catch {
      setDetailQr(null)
    }
  }

  async function runAction(
    fn: (id: string) => Promise<void>,
  ) {
    if (!selectedId)
      return
    setBusy(true)
    setError(null)
    try {
      await fn(selectedId)
      await loadTickets()
      await refreshDetail(selectedId)
    } catch {
      setError('Falha na operação.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <PermissionGate
      anyOf={[
        ApplicationPermissions.IngressosVisualizar,
        ApplicationPermissions.IngressosGerenciar,
      ]}
    >
      <h1>Ingressos</h1>
      <p style={{ color: '#555' }}>Total: {totalCount}</p>
      {loading ? <p>Carregando...</p> : null}
      {error ? <p style={{ color: 'crimson' }}>{error}</p> : null}

      {canManage ? (
        <section style={{ marginBottom: 24, padding: 12, background: '#f5f5f5', borderRadius: 8 }}>
          <h2>Reservar</h2>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, alignItems: 'flex-end' }}>
            <label>
              UserId
              <input value={userId} onChange={(e) => setUserId(e.target.value)} placeholder="GUID do torcedor" style={{ width: 280 }} />
            </label>
            <label>
              Jogo
              <select value={gameId} onChange={(e) => setGameId(e.target.value)} style={{ minWidth: 220 }}>
                {games.map((g) => (
                  <option key={g.gameId} value={g.gameId}>
                    {g.opponent} — {g.competition}
                  </option>
                ))}
              </select>
            </label>
            <button type="button" disabled={busy} onClick={() => void onReserve()}>
              Reservar
            </button>
          </div>
        </section>
      ) : (
        <p style={{ color: '#666' }}>
          Somente leitura: falta Ingressos.Gerenciar para reservar e alterar status.
        </p>
      )}

      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12, marginBottom: 12 }}>
        <label>
          Filtro status
          <input value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)} placeholder="Reserved | Purchased | Redeemed" />
        </label>
        <button type="button" onClick={() => void loadTickets()}>
          Atualizar lista
        </button>
      </div>

      <div style={{ display: 'flex', gap: 24, flexWrap: 'wrap' }}>
        <div style={{ flex: '2 1 400px' }}>
          <h2>Lista</h2>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 14 }}>
            <thead>
              <tr style={{ textAlign: 'left', borderBottom: '1px solid #ccc' }}>
                <th>Seleção</th>
                <th>Status</th>
                <th>Torcedor</th>
                <th>Jogo</th>
                <th>Criado</th>
              </tr>
            </thead>
            <tbody>
              {items.map((t) => (
                <tr key={t.ticketId} style={{ borderBottom: '1px solid #eee' }}>
                  <td>
                    <input
                      type="radio"
                      name="ticketPick"
                      checked={selectedId === t.ticketId}
                      onChange={() => {
                        setSelectedId(t.ticketId)
                        void refreshDetail(t.ticketId)
                      }}
                    />
                  </td>
                  <td>{t.status}</td>
                  <td>{t.userEmail}</td>
                  <td>
                    {t.opponent} ({new Date(t.gameDate).toLocaleDateString()})
                  </td>
                  <td>{new Date(t.createdAt).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <div style={{ flex: '1 1 260px' }}>
          <h2>Ações</h2>
          {canManage ? (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
              <button type="button" disabled={busy || !selectedId} onClick={() => void runAction(purchaseAdminTicket)}>
                Comprar / confirmar
              </button>
              <button type="button" disabled={busy || !selectedId} onClick={() => void runAction(syncAdminTicket)}>
                Sincronizar com provedor
              </button>
              <button type="button" disabled={busy || !selectedId} onClick={() => void runAction(redeemAdminTicket)}>
                Resgatar
              </button>
            </div>
          ) : (
            <p style={{ color: '#666' }}>Requer Ingressos.Gerenciar.</p>
          )}
          <h3 style={{ marginTop: 16 }}>QR / payload</h3>
          <pre style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-all', background: '#fafafa', padding: 8 }}>
            {detailQr ?? '—'}
          </pre>
        </div>
      </div>
    </PermissionGate>
  )
}
