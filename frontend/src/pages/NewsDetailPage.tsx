import { useEffect, useState } from 'react'
import { Link, NavLink } from 'react-router-dom'
import { useParams } from 'react-router-dom'
import { ArrowLeft, Newspaper, Home, Calendar, CreditCard, User } from 'lucide-react'
import { getTorcedorNewsDetail, type TorcedorNewsDetail } from '../features/torcedor/torcedorNewsApi'
import './AppShell.css'

const BOTTOM_NAV = [
  { to: '/dashboard', label: 'Início', icon: <Home size={22} /> },
  { to: '/news', label: 'Notícias', icon: <Newspaper size={22} /> },
  { to: '/games', label: 'Jogos', icon: <Calendar size={22} /> },
  { to: '/digital-card', label: 'Carteirinha', icon: <CreditCard size={22} /> },
  { to: '/account', label: 'Conta', icon: <User size={22} /> },
]

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('pt-BR', {
    weekday: 'long',
    day: '2-digit',
    month: 'long',
    year: 'numeric',
  })
}

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
    return () => { cancelled = true }
  }, [newsId])

  return (
    <div className="news-root">
      <header className="news-header">
        <Link to="/news" className="news-header__back" aria-label="Voltar às notícias">
          <ArrowLeft size={18} />
        </Link>
        <h1 className="news-header__title">
          {detail ? detail.title : 'Notícias'}
        </h1>
      </header>

      <main style={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
        {loading ? (
          <div style={{ padding: '1.25rem 1rem' }}>
            <div className="news-skeleton-hero" style={{ aspectRatio: '16/7' }} />
            <div style={{ marginTop: '1.5rem', display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
              <div className="news-skeleton-card" style={{ height: 32, width: '40%' }} />
              <div className="news-skeleton-card" style={{ height: 48 }} />
              <div className="news-skeleton-card" style={{ height: 24, width: '80%' }} />
              <div className="news-skeleton-card" style={{ height: 120 }} />
            </div>
          </div>
        ) : null}

        {error ? (
          <div style={{ padding: '1.25rem 1rem' }}>
            <p className="news-error">{error}</p>
            <Link to="/news" className="news-header__back" style={{ display: 'inline-flex', gap: '0.4rem', alignItems: 'center', color: '#81e592', fontSize: '0.9rem', textDecoration: 'none' }}>
              <ArrowLeft size={16} />
              Ver todas as notícias
            </Link>
          </div>
        ) : null}

        {detail ? (
          <article className="news-article">
            <div
              className="news-article__image"
              style={{ background: 'linear-gradient(135deg, #1a3828 0%, #0d2218 100%)' }}
            >
              <Newspaper size={52} color="rgba(129,229,146,0.15)" />
            </div>

            <p className="news-article__date">{formatDate(detail.publishedAt)}</p>
            <h2 className="news-article__title">{detail.title}</h2>

            {detail.summary ? (
              <p className="news-article__lead">{detail.summary}</p>
            ) : null}

            <div className="news-article__content">{detail.content}</div>
          </article>
        ) : null}
      </main>

      <nav className="dash-bottom-nav" aria-label="Navegação principal">
        {BOTTOM_NAV.map(item => (
          <NavLink
            key={item.to}
            to={item.to}
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
