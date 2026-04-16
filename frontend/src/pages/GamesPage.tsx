import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import { resolvePublicAssetUrl } from '../features/account/accountApi'
import { listTorcedorGames, type TorcedorGameListItem } from '../features/torcedor/torcedorGamesApi'
import { TorcedorBottomNav } from '../shared/torcedorBottomNav'
import './AppShell.css'

export function GamesPage() {
  const [items, setItems] = useState<TorcedorGameListItem[]>([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

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

      <main className="subpage-content">
        {loading ? <p className="app-muted">Carregando…</p> : null}
        {error ? <p role="alert" style={{ color: '#ffc6c6', fontSize: '0.9rem' }}>{error}</p> : null}
        {!loading && items.length === 0 && !error ? (
          <p className="app-muted">Nenhum jogo disponível no momento.</p>
        ) : null}
        <ul className="game-list">
          {items.map(g => (
            <li key={g.gameId} className="game-card">
              <div className="game-card__head">
                {g.opponentLogoUrl
                  ? (
                      <img
                        className="game-card__logo"
                        src={resolvePublicAssetUrl(g.opponentLogoUrl) ?? ''}
                        alt=""
                      />
                    )
                  : null}
                <p className="game-card__opponent">{g.opponent}</p>
              </div>
              <div className="game-card__meta">
                <span className="game-card__competition">{g.competition}</span>
                <span className="game-card__date">
                  {new Date(g.gameDate).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })}
                </span>
              </div>
            </li>
          ))}
        </ul>
      </main>

      <TorcedorBottomNav />
    </div>
  )
}
