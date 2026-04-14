import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getTorcedorNewsDetail, type TorcedorNewsDetail } from '../features/torcedor/torcedorNewsApi'

export function NewsDetailPage() {
  const { newsId } = useParams<{ newsId: string }>()
  const [detail, setDetail] = useState<TorcedorNewsDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!newsId)
      return
    let cancelled = false
    void (async () => {
      try {
        setLoading(true)
        const d = await getTorcedorNewsDetail(newsId)
        if (!cancelled) {
          setDetail(d)
          setError(null)
        }
      }
      catch (e: unknown) {
        if (!cancelled) {
          const status = (e as { response?: { status?: number } })?.response?.status
          if (status === 404)
            setError('Notícia não encontrada ou não publicada.')
          else
            setError(e instanceof Error ? e.message : 'Erro ao carregar')
          setDetail(null)
        }
      }
      finally {
        if (!cancelled)
          setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [newsId])

  return (
    <main style={{ maxWidth: 720, margin: '2rem auto', fontFamily: 'system-ui' }}>
      <p>
        <Link to="/news">← Notícias</Link>
      </p>
      {loading ? <p>Carregando…</p> : null}
      {error ? <p style={{ color: '#721c24' }}>{error}</p> : null}
      {detail ? (
        <article>
          <h1 style={{ marginBottom: '0.5rem' }}>{detail.title}</h1>
          <p style={{ fontSize: '0.9rem', color: '#666', marginTop: 0 }}>
            {new Date(detail.publishedAt).toLocaleString()}
          </p>
          {detail.summary ? <p style={{ fontWeight: 500 }}>{detail.summary}</p> : null}
          <div style={{ whiteSpace: 'pre-wrap', lineHeight: 1.6 }}>{detail.content}</div>
        </article>
      ) : null}
    </main>
  )
}
