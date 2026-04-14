import { useEffect, useMemo, useState } from 'react'
import { Link, NavLink } from 'react-router-dom'
import { ArrowLeft, Trophy, Home, Newspaper, Calendar, CreditCard, User } from 'lucide-react'
import { useAuth } from '../features/auth/AuthContext'
import {
  getAllTimeLoyaltyRanking,
  getMonthlyLoyaltyRanking,
  getMyLoyaltySummary,
  type TorcedorLoyaltyRankingPage,
  type TorcedorLoyaltySummary,
} from '../features/torcedor/torcedorLoyaltyApi'
import './AppShell.css'

const BOTTOM_NAV = [
  { to: '/', label: 'Início', icon: <Home size={22} /> },
  { to: '/news', label: 'Notícias', icon: <Newspaper size={22} /> },
  { to: '/games', label: 'Jogos', icon: <Calendar size={22} /> },
  { to: '/digital-card', label: 'Carteirinha', icon: <CreditCard size={22} /> },
  { to: '/account', label: 'Conta', icon: <User size={22} /> },
]

function RankingBlock(props: {
  title: string
  page: TorcedorLoyaltyRankingPage | null
  currentUserId: string | undefined
}) {
  const { title, page, currentUserId } = props
  if (!page)
    return null

  return (
    <div className="loyalty-ranking">
      <p className="loyalty-ranking__title">{title}</p>
      {page.totalCount === 0 ? (
        <p className="loyalty-ranking__empty">Nenhum ponto registrado neste período.</p>
      ) : (
        <ol className="loyalty-ranking__list">
          {page.items.map((row) => {
            const isMe = !!(currentUserId && row.userId === currentUserId)
            return (
              <li
                key={`${row.rank}-${row.userId}`}
                className={`loyalty-ranking__row${isMe ? ' loyalty-ranking__row--me' : ''}`}
              >
                <span className="loyalty-ranking__rank">#{row.rank}</span>
                <span className="loyalty-ranking__name">{row.userName || '(sem nome)'}{isMe ? ' (você)' : null}</span>
                <span className="loyalty-ranking__pts">{row.totalPoints} pts</span>
              </li>
            )
          })}
        </ol>
      )}
      {page.me ? (
        <p className="loyalty-ranking__me-pos">
          Sua posição: <strong>#{page.me.rank}</strong> — {page.me.totalPoints} pts
        </p>
      ) : (
        <p className="loyalty-ranking__me-pos">
          Você ainda não aparece no ranking deste período.
        </p>
      )}
    </div>
  )
}

export function LoyaltyPage() {
  const { user } = useAuth()
  const [summary, setSummary] = useState<TorcedorLoyaltySummary | null>(null)
  const [monthly, setMonthly] = useState<TorcedorLoyaltyRankingPage | null>(null)
  const [allTime, setAllTime] = useState<TorcedorLoyaltyRankingPage | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const periodLabel = useMemo(() => {
    const now = new Date()
    const y = now.getUTCFullYear()
    const m = now.getUTCMonth() + 1
    return `${String(m).padStart(2, '0')}/${y} (UTC)`
  }, [])

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        setLoading(true)
        const now = new Date()
        const year = now.getUTCFullYear()
        const month = now.getUTCMonth() + 1
        const [s, mPage, aPage] = await Promise.all([
          getMyLoyaltySummary(),
          getMonthlyLoyaltyRanking({ year, month, pageSize: 50 }),
          getAllTimeLoyaltyRanking({ pageSize: 50 }),
        ])
        if (!cancelled) {
          setSummary(s)
          setMonthly(mPage)
          setAllTime(aPage)
          setError(null)
        }
      }
      catch (e) {
        if (!cancelled)
          setError(e instanceof Error ? e.message : 'Erro ao carregar fidelidade')
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
    <div className="loyalty-root">
      <header className="subpage-header">
        <Link to="/" className="subpage-header__back" aria-label="Voltar">
          <ArrowLeft size={18} />
        </Link>
        <h1 className="subpage-header__title">
          <Trophy size={16} style={{ marginRight: '0.4rem', verticalAlign: 'middle' }} />
          Fidelidade
        </h1>
      </header>

      <main className="subpage-content">
        {loading ? <p className="app-muted">Carregando…</p> : null}
        {error ? (
          <p role="alert" style={{ color: '#ffc6c6', fontSize: '0.9rem' }}>{error}</p>
        ) : null}
        {!loading && !error && summary ? (
          <>
            <div className="loyalty-summary">
              <div className="loyalty-summary__row">
                <span className="loyalty-summary__label">Saldo total:</span>
                <span className="loyalty-summary__value loyalty-summary__value--accent">
                  {summary.totalPoints} pts
                </span>
              </div>
              <div className="loyalty-summary__row">
                <span className="loyalty-summary__label">Pontos no mês ({periodLabel}):</span>
                <span className="loyalty-summary__value">{summary.monthlyPoints} pts</span>
              </div>
              <div className="loyalty-summary__row">
                <span className="loyalty-summary__label">Posição no mês:</span>
                <span className="loyalty-summary__value">
                  {summary.monthlyRank != null ? `#${summary.monthlyRank}` : '—'}
                </span>
              </div>
              <div className="loyalty-summary__row">
                <span className="loyalty-summary__label">Posição geral:</span>
                <span className="loyalty-summary__value">
                  {summary.allTimeRank != null ? `#${summary.allTimeRank}` : '—'}
                </span>
              </div>
            </div>
            <RankingBlock
              title={`Ranking do mês (${periodLabel})`}
              page={monthly}
              currentUserId={user?.id}
            />
            <RankingBlock title="Ranking geral (todos os tempos)" page={allTime} currentUserId={user?.id} />
          </>
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
