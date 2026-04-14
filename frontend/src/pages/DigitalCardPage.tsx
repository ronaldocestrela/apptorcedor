import { useEffect, useState } from 'react'
import { Link, NavLink } from 'react-router-dom'
import { ArrowLeft, CreditCard, Home, Newspaper, Calendar, User } from 'lucide-react'
import {
  getMyDigitalCardWithSource,
  type MyDigitalCardView,
  type MyDigitalCardViewState,
} from '../features/torcedor/torcedorDigitalCardApi'
import './AppShell.css'

const BOTTOM_NAV = [
  { to: '/', label: 'Início', icon: <Home size={22} /> },
  { to: '/news', label: 'Notícias', icon: <Newspaper size={22} /> },
  { to: '/games', label: 'Jogos', icon: <Calendar size={22} /> },
  { to: '/digital-card', label: 'Carteirinha', icon: <CreditCard size={22} /> },
  { to: '/account', label: 'Conta', icon: <User size={22} /> },
]

function stateLabel(state: MyDigitalCardViewState): string {
  switch (state) {
    case 'NotAssociated':
      return 'Sem associação ativa'
    case 'MembershipInactive':
      return 'Associação não elegível'
    case 'AwaitingIssuance':
      return 'Aguardando emissão'
    case 'Active':
      return 'Carteirinha ativa'
    default:
      return state
  }
}

export function DigitalCardPage() {
  const [data, setData] = useState<MyDigitalCardView | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [fromCache, setFromCache] = useState(false)

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        setLoading(true)
        setFromCache(false)
        const { data: view, fromCache } = await getMyDigitalCardWithSource({ allowStaleOnNetworkError: true })
        if (!cancelled) {
          setData(view)
          setFromCache(fromCache)
          setError(null)
        }
      }
      catch (e) {
        if (!cancelled)
          setError(e instanceof Error ? e.message : 'Erro ao carregar carteirinha')
      }
      finally {
        if (!cancelled)
          setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  return (
    <div className="digital-card-root">
      <header className="subpage-header">
        <Link to="/" className="subpage-header__back" aria-label="Voltar">
          <ArrowLeft size={18} />
        </Link>
        <h1 className="subpage-header__title">Carteirinha digital</h1>
      </header>

      <main className="subpage-content">
        {loading ? <p className="app-muted">Carregando…</p> : null}
        {error ? <p role="alert" style={{ color: '#ffc6c6', fontSize: '0.9rem' }}>{error}</p> : null}
        {!loading && !error && data ? (
          <div className="digital-card-display">
            <p className="digital-card-display__state">
              {stateLabel(data.state)}
              {fromCache ? (
                <span style={{ marginLeft: '0.5rem', fontSize: '0.78rem', fontWeight: 400, color: '#6a9c78' }}>
                  (cache local)
                </span>
              ) : null}
            </p>
            <p className="digital-card-display__sub">
              Associação: {data.membershipStatus}
            </p>
            {data.message ? (
              <p className="digital-card-message">{data.message}</p>
            ) : null}
            {data.state === 'Active' && data.templatePreviewLines?.length ? (
              <div className="digital-card-display__template">
                {data.templatePreviewLines.join('\n')}
              </div>
            ) : null}
            {data.state === 'Active' && data.verificationToken ? (
              <p className="digital-card-display__token">
                <span style={{ color: '#9db7a7' }}>Token de verificação: </span>
                {data.verificationToken}
              </p>
            ) : null}
            {data.cacheValidUntilUtc ? (
              <p className="digital-card-cache-note">
                Cache até {new Date(data.cacheValidUntilUtc).toLocaleString('pt-BR')}. Dados podem ser usados offline nesse período.
              </p>
            ) : null}
          </div>
        ) : null}
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

  const [data, setData] = useState<MyDigitalCardView | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [fromCache, setFromCache] = useState(false)

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        setLoading(true)
        setFromCache(false)
        const { data: view, fromCache } = await getMyDigitalCardWithSource({ allowStaleOnNetworkError: true })
        if (!cancelled) {
          setData(view)
          setFromCache(fromCache)
          setError(null)
        }
      }
      catch (e) {
        if (!cancelled)
          setError(e instanceof Error ? e.message : 'Erro ao carregar carteirinha')
      }
      finally {
        if (!cancelled)
          setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  return (
    <main style={{ maxWidth: 560, margin: '2rem auto', fontFamily: 'system-ui' }}>
      <p>
        <Link to="/">← Início</Link>
      </p>
      <h1>Carteirinha digital</h1>
      <p style={{ color: '#555' }}>
        Exibição para o torcedor autenticado. O token de verificação pertence à sua emissão ativa.
      </p>
      {loading ? <p>Carregando…</p> : null}
      {error ? <p style={{ color: '#721c24' }}>{error}</p> : null}
      {!loading && !error && data ? (
        <>
          <p style={{ marginBottom: 0 }}>
            <strong>{stateLabel(data.state)}</strong>
            {fromCache ? (
              <span style={{ marginLeft: 8, fontSize: '0.85rem', color: '#666' }}>
                (dados em cache local)
              </span>
            ) : null}
          </p>
          <p style={{ marginTop: 4, fontSize: '0.9rem', color: '#555' }}>
            Status da associação (sistema):
            {' '}
            {data.membershipStatus}
          </p>
          {data.message ? <p style={{ color: '#856404' }}>{data.message}</p> : null}
          {data.state === 'Active' && data.templatePreviewLines?.length ? (
            <section
              style={{
                marginTop: '1.25rem',
                padding: '1rem',
                border: '1px solid #ccc',
                borderRadius: 8,
                background: '#fafafa',
                fontFamily: 'ui-monospace, monospace',
                fontSize: '0.9rem',
                whiteSpace: 'pre-wrap',
              }}
            >
              {data.templatePreviewLines.join('\n')}
            </section>
          ) : null}
          {data.state === 'Active' && data.verificationToken ? (
            <p style={{ marginTop: '1rem', fontSize: '0.85rem', wordBreak: 'break-all' }}>
              <strong>Token de verificação:</strong>
              {' '}
              {data.verificationToken}
            </p>
          ) : null}
          {data.cacheValidUntilUtc ? (
            <p style={{ marginTop: '1rem', fontSize: '0.75rem', color: '#888' }}>
              Cache sugerido pela API até
              {' '}
              {new Date(data.cacheValidUntilUtc).toLocaleString()}
              . Os dados podem ser guardados localmente nesse período para uso offline limitado.
            </p>
          ) : null}
        </>
      ) : null}
    </main>
  )
}
