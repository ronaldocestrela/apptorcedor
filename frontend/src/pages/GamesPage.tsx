import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { listTorcedorGames, type TorcedorGameListItem } from '../features/torcedor/torcedorGamesApi'

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
    <main style={{ maxWidth: 640, margin: '2rem auto', fontFamily: 'system-ui' }}>
      <p>
        <Link to="/">← Início</Link>
      </p>
      <h1>Jogos</h1>
      <p style={{ color: '#555', fontSize: '0.9rem' }}>
        Partidas ativas divulgadas pelo clube (somente leitura).
      </p>
      {loading ? <p>Carregando…</p> : null}
      {error ? <p style={{ color: '#721c24' }}>{error}</p> : null}
      {!loading && !error ? (
        <p style={{ color: '#555' }}>
          {total}
          {' '}
          jogo(s)
        </p>
      ) : null}
      <ul style={{ listStyle: 'none', padding: 0 }}>
        {items.map(g => (
          <li key={g.gameId} style={{ marginBottom: '1rem', borderBottom: '1px solid #eee', paddingBottom: '0.75rem' }}>
            <strong>
              {g.opponent}
            </strong>
            <span style={{ color: '#666', marginLeft: 8 }}>
              —
              {g.competition}
            </span>
            <p style={{ margin: '0.35rem 0 0', fontSize: '0.9rem', color: '#444' }}>
              {new Date(g.gameDate).toLocaleString()}
            </p>
          </li>
        ))}
      </ul>
      {!loading && items.length === 0 ? <p>Nenhum jogo disponível no momento.</p> : null}
    </main>
  )
}
