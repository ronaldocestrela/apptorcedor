import { useCallback, useEffect, useState } from 'react'
import { Link, NavLink } from 'react-router-dom'
import { ArrowLeft, Ticket, Home, Newspaper, Calendar, CreditCard, User } from 'lucide-react'
import {
  listMyTickets,
  redeemMyTicket,
  type TorcedorTicketListItem,
} from '../features/torcedor/torcedorTicketsApi'
import './AppShell.css'

const BOTTOM_NAV = [
  { to: '/', label: 'Início', icon: <Home size={22} /> },
  { to: '/news', label: 'Notícias', icon: <Newspaper size={22} /> },
  { to: '/games', label: 'Jogos', icon: <Calendar size={22} /> },
  { to: '/digital-card', label: 'Carteirinha', icon: <CreditCard size={22} /> },
  { to: '/account', label: 'Conta', icon: <User size={22} /> },
]

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

      <nav className="dash-bottom-nav" aria-label="Navegação principal">
        {BOTTOM_NAV.map(item => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.to === '/'}
            className={({ isActive }) =>
              `dash-bottom-nav__item${isActive ? ' active' : ''}`
            }
          >
            {item.icon}
            {item.label}
          </NavLink>
        ))}
      </nav>
    </div>
  )
}

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
    }
    catch (e) {
      setActionError(e instanceof Error ? e.message : 'Não foi possível resgatar')
    }
    finally {
      setActionId(null)
    }
  }

  return (
    <main style={{ maxWidth: 640, margin: '2rem auto', fontFamily: 'system-ui' }}>
      <p>
        <Link to="/">← Início</Link>
      </p>
      <h1>Meus ingressos</h1>
      {loading ? <p>Carregando…</p> : null}
      {error ? <p style={{ color: '#721c24' }}>{error}</p> : null}
      {actionError ? <p style={{ color: '#721c24' }}>{actionError}</p> : null}
      {!loading && !error ? (
        <p style={{ color: '#555' }}>
          {total}
          {' '}
          ingresso(s)
        </p>
      ) : null}
      <ul style={{ listStyle: 'none', padding: 0 }}>
        {items.map(t => (
          <li
            key={t.ticketId}
            style={{
              marginBottom: '1.25rem',
              borderBottom: '1px solid #eee',
              paddingBottom: '1rem',
            }}
          >
            <div>
              <strong>{t.opponent}</strong>
              <span style={{ color: '#666', marginLeft: 8 }}>{t.competition}</span>
            </div>
            <p style={{ margin: '0.35rem 0 0', fontSize: '0.9rem' }}>
              Partida:
              {' '}
              {new Date(t.gameDate).toLocaleString()}
            </p>
            <p style={{ margin: '0.25rem 0 0', fontSize: '0.9rem' }}>
              Status:
              {' '}
              <strong>{t.status}</strong>
            </p>
            {t.qrCode ? (
              <p style={{ margin: '0.5rem 0 0', fontSize: '0.85rem', wordBreak: 'break-all', color: '#333' }}>
                QR:
                {' '}
                {t.qrCode}
              </p>
            ) : null}
            {t.status === 'Purchased' ? (
              <p style={{ margin: '0.75rem 0 0' }}>
                <button
                  type="button"
                  disabled={actionId === t.ticketId}
                  onClick={() => void onRedeem(t.ticketId)}
                >
                  {actionId === t.ticketId ? 'Resgatando…' : 'Resgatar ingresso'}
                </button>
              </p>
            ) : null}
          </li>
        ))}
      </ul>
      {!loading && items.length === 0 ? <p>Você ainda não possui ingressos vinculados à sua conta.</p> : null}
    </main>
  )
}
