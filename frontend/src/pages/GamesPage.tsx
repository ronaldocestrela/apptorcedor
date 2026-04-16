import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import { resolvePublicAssetUrl } from '../features/account/accountApi'
import { listTorcedorGames, type TorcedorGameListItem } from '../features/torcedor/torcedorGamesApi'
import { getPublicBranding } from '../shared/branding/brandingApi'
import { getTeamShieldPlaceholderDataUrl } from '../shared/branding/teamShieldPlaceholder'
import { TorcedorBottomNav } from '../shared/torcedorBottomNav'
import './AppShell.css'

const CLUB_SHORT =
  (import.meta.env.VITE_CLUB_SHORT_NAME as string | undefined)?.trim() || 'FFC'

type DayGroup = {
  dateKey: string
  label: string
  games: TorcedorGameListItem[]
}

function toDateKeyLocal(iso: string): string {
  const d = new Date(iso)
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

function formatDayLabel(iso: string): string {
  return new Date(iso).toLocaleDateString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  })
}

function formatKickoff(iso: string): string {
  const d = new Date(iso)
  const m = d.getMinutes()
  if (m === 0)
    return `${d.getHours()}h`
  return d.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
}

function groupGamesByDay(sorted: TorcedorGameListItem[]): DayGroup[] {
  const map = new Map<string, TorcedorGameListItem[]>()
  for (const g of sorted) {
    const key = toDateKeyLocal(g.gameDate)
    const list = map.get(key) ?? []
    list.push(g)
    map.set(key, list)
  }
  return Array.from(map.entries()).map(([dateKey, games]) => ({
    dateKey,
    label: formatDayLabel(games[0]!.gameDate),
    games,
  }))
}

function pickFeaturedGameId(sorted: TorcedorGameListItem[]): string | null {
  if (sorted.length === 0)
    return null
  const now = Date.now()
  const next = sorted.find(g => new Date(g.gameDate).getTime() >= now)
  return (next ?? sorted[0])!.gameId
}

export function GamesPage() {
  const [items, setItems] = useState<TorcedorGameListItem[]>([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [clubShieldSrc, setClubShieldSrc] = useState(() => getTeamShieldPlaceholderDataUrl())

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const b = await getPublicBranding()
        const resolved = resolvePublicAssetUrl(b.teamShieldUrl ?? undefined)
        if (!cancelled && resolved)
          setClubShieldSrc(resolved)
      }
      catch {
        /* keep placeholder */
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        setLoading(true)
        const page = await listTorcedorGames({ pageSize: 50 })
        if (!cancelled) {
          setItems(page.items)
          setTotal(page.totalCount)
          setError(null)
        }
      }
      catch (e) {
        if (!cancelled)
          setError(e instanceof Error ? e.message : 'Erro ao carregar jogos')
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

  const sortedItems = useMemo(
    () =>
      [...items].sort(
        (a, b) => new Date(a.gameDate).getTime() - new Date(b.gameDate).getTime(),
      ),
    [items],
  )

  const featuredId = useMemo(() => pickFeaturedGameId(sortedItems), [sortedItems])
  const dayGroups = useMemo(() => groupGamesByDay(sortedItems), [sortedItems])

  return (
    <div className="games-root">
      <header className="subpage-header">
        <Link to="/" className="subpage-header__back" aria-label="Voltar">
          <ArrowLeft size={18} />
        </Link>
        <h1 className="subpage-header__title">Jogos</h1>
        {!loading && !error ? (
          <span className="subpage-header__badge">{total}</span>
        ) : null}
      </header>

      <main className="subpage-content games-page">
        {loading ? <p className="app-muted">Carregando…</p> : null}
        {error ? (
          <p className="games-page__error" role="alert">
            {error}
          </p>
        ) : null}
        {!loading && items.length === 0 && !error ? (
          <p className="app-muted">Nenhum jogo disponível no momento.</p>
        ) : null}

        <div className="games-schedule">
          {dayGroups.map(group => {
            const showNextBadge = group.games.some(g => g.gameId === featuredId)
            return (
              <section
                key={group.dateKey}
                className="games-day"
                aria-labelledby={`games-day-${group.dateKey}`}
              >
                <div className="games-day__header">
                  <h2 id={`games-day-${group.dateKey}`} className="games-day__date">
                    {group.label}
                  </h2>
                  {showNextBadge ? (
                    <span className="games-day__badge">Evento Próximo</span>
                  ) : null}
                </div>
                <ul className="games-day__list">
                  {group.games.map(g => {
                    const isActive = g.gameId === featuredId
                    const subtitle = `${g.competition} - ${formatKickoff(g.gameDate)}`
                    const opponentSrc = resolvePublicAssetUrl(g.opponentLogoUrl ?? undefined)
                    return (
                      <li
                        key={g.gameId}
                        className={`game-card-ev${isActive ? ' game-card-ev--active' : ' game-card-ev--muted'}`}
                      >
                        <div className="game-card-ev__body">
                          <p className="game-card-ev__title">
                            {CLUB_SHORT}
                            {' '}
                            <span className="game-card-ev__title-sep">x</span>
                            {' '}
                            {g.opponent}
                          </p>
                          <p className="game-card-ev__subtitle">{subtitle}</p>
                          <div className="game-card-ev__logos" aria-hidden="true">
                            <div className="game-card-ev__logo-slot game-card-ev__logo-slot--home">
                              <img
                                className="game-card-ev__shield"
                                src={clubShieldSrc}
                                alt=""
                              />
                            </div>
                            <span className="game-card-ev__vs">x</span>
                            <div className="game-card-ev__logo-slot game-card-ev__logo-slot--away">
                              {opponentSrc
                                ? (
                                    <img
                                      className="game-card-ev__opponent-logo"
                                      src={opponentSrc}
                                      alt=""
                                    />
                                  )
                                : (
                                    <span className="game-card-ev__opponent-fallback">
                                      {g.opponent.slice(0, 3).toUpperCase()}
                                    </span>
                                  )}
                            </div>
                          </div>
                          <div className="game-card-ev__cta-wrap">
                            <button
                              type="button"
                              className="game-card-ev__cta"
                              disabled={!isActive}
                            >
                              Ingresso disponível
                            </button>
                          </div>
                        </div>
                      </li>
                    )
                  })}
                </ul>
              </section>
            )
          })}
        </div>
      </main>

      <TorcedorBottomNav />
    </div>
  )
}
