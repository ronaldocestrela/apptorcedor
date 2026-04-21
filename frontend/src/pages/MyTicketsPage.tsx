import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeft, Ticket } from 'lucide-react'
import {
  listMyTickets,
  redeemMyTicket,
  type TorcedorTicketListItem,
} from '../features/torcedor/torcedorTicketsApi'
import { TorcedorBottomNav } from '../shared/torcedorBottomNav'
import { TORCEDOR_INGRESSO_REDEEM_TOAST_KEY } from '../shared/torcedorIngressoToast'
import './AppShell.css'

export function MyTicketsPage() {
  const [items, setItems] = useState<TorcedorTicketListItem[]>([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionId, setActionId] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const reload = useCallback(async () => {
    const page = await listMyTickets({ pageSize: 50 })
    setItems(page.items)
    setTotal(page.totalCount)
  }, [])

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        setLoading(true)
        await reload()
        if (!cancelled)
          setError(null)
      }
      catch (e) {
        if (!cancelled)
          setError(e instanceof Error ? e.message : 'Erro ao carregar ingressos')
      }
      finally {
        if (!cancelled)
          setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [reload])

  async function onRedeem(ticketId: string) {
    setActionError(null)
    setActionId(ticketId)
    try {
      await redeemMyTicket(ticketId)
      await reload()
      try {
        sessionStorage.setItem(TORCEDOR_INGRESSO_REDEEM_TOAST_KEY, '1')
      }
      catch {
        /* ignore private mode / quota */
      }
    }
    catch (e) {
      setActionError(e instanceof Error ? e.message : 'Não foi possível resgatar')
    }
    finally {
      setActionId(null)
    }
  }

  return (
    <div className="tickets-root">
      <header className="subpage-header">
        <Link to="/" className="subpage-header__back" aria-label="Voltar">
          <ArrowLeft size={18} />
        </Link>
        <h1 className="subpage-header__title">
          <Ticket size={16} style={{ marginRight: '0.4rem', verticalAlign: 'middle' }} />
          Meus ingressos
        </h1>
        {!loading && !error ? (
          <span className="subpage-header__badge">{total}</span>
        ) : null}
      </header>

      <main className="subpage-content">
        {loading ? <p className="app-muted">Carregando…</p> : null}
        {error ? <p role="alert" style={{ color: '#ffc6c6', fontSize: '0.9rem' }}>{error}</p> : null}
        {actionError ? <p role="alert" style={{ color: '#ffc6c6', fontSize: '0.9rem' }}>{actionError}</p> : null}
        {!loading && items.length === 0 && !error ? (
          <p className="app-muted">Você ainda não possui ingressos vinculados à sua conta.</p>
        ) : null}
        <ul className="ticket-list">
          {items.map(t => (
            <li key={t.ticketId} className="ticket-card">
              <p className="ticket-card__opponent">{t.opponent}</p>
              <div className="ticket-card__meta">
                <span className="ticket-card__competition">{t.competition}</span>
                <span className="ticket-card__date">
                  {new Date(t.gameDate).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })}
                </span>
              </div>
              <p className="ticket-card__status">
                Status: <strong>{t.status}</strong>
              </p>
              {t.qrCode ? (
                <p className="ticket-card__qr">QR: {t.qrCode}</p>
              ) : null}
              {t.status === 'Purchased' ? (
                <div className="ticket-card__redeem">
                  <button
                    type="button"
                    className="btn-primary"
                    disabled={actionId === t.ticketId}
                    onClick={() => void onRedeem(t.ticketId)}
                  >
                    {actionId === t.ticketId ? 'Resgatando…' : 'Resgatar ingresso'}
                  </button>
                </div>
              ) : null}
            </li>
          ))}
        </ul>
      </main>

      <TorcedorBottomNav />
    </div>
  )
}
