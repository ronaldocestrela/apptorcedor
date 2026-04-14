import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { listTorcedorNewsFeed, type TorcedorNewsFeedItem } from '../features/torcedor/torcedorNewsApi'

export function NewsFeedPage() {
  const [items, setItems] = useState<TorcedorNewsFeedItem[]>([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        setLoading(true)
        const page = await listTorcedorNewsFeed({ pageSize: 50 })
        if (!cancelled) {
          setItems(page.items)
          setTotal(page.totalCount)
          setError(null)
        }
      }
      catch (e) {
        if (!cancelled)
          setError(e instanceof Error ? e.message : 'Erro ao carregar notícias')
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
      <h1>Notícias</h1>
      {loading ? <p>Carregando…</p> : null}
      {error ? <p style={{ color: '#721c24' }}>{error}</p> : null}
      {!loading && !error ? (
        <p style={{ color: '#555' }}>
          {total}
          {' '}
          publicação(ões)
        </p>
      ) : null}
      <ul style={{ listStyle: 'none', padding: 0 }}>
        {items.map(item => (
          <li key={item.newsId} style={{ marginBottom: '1rem', borderBottom: '1px solid #eee', paddingBottom: '0.75rem' }}>
            <Link to={`/news/${item.newsId}`} style={{ fontWeight: 600 }}>
              {item.title}
            </Link>
            {item.summary ? <p style={{ margin: '0.35rem 0 0', color: '#444' }}>{item.summary}</p> : null}
            <p style={{ margin: '0.25rem 0 0', fontSize: '0.85rem', color: '#888' }}>
              {new Date(item.publishedAt).toLocaleString()}
            </p>
          </li>
        ))}
      </ul>
      {!loading && items.length === 0 ? <p>Nenhuma notícia publicada no momento.</p> : null}
    </main>
  )
}
