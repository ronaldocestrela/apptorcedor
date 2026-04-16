import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeft, Newspaper } from 'lucide-react'
import { listTorcedorNewsFeed, type TorcedorNewsFeedItem } from '../features/torcedor/torcedorNewsApi'
import { TorcedorBottomNav } from '../shared/torcedorBottomNav'
import './AppShell.css'

const CARD_GRADIENTS = [
  'linear-gradient(135deg, #1a3828 0%, #0d2218 100%)',
  'linear-gradient(150deg, #1b3328 0%, #0e2016 100%)',
  'linear-gradient(120deg, #1d3a28 0%, #101c14 100%)',
  'linear-gradient(160deg, #172e22 0%, #0c1a10 100%)',
  'linear-gradient(140deg, #1e3526 0%, #0f1e14 100%)',
]

function cardGradient(index: number) {
  return CARD_GRADIENTS[index % CARD_GRADIENTS.length]
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('pt-BR', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  })
}

function NewsSkeletons() {
  return (
    <>
      <div className="news-skeleton-hero" />
      <div className="news-grid">
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="news-skeleton-card" />
        ))}
      </div>
    </>
  )
}

function HeroCard({ item, index }: { item: TorcedorNewsFeedItem; index: number }) {
  return (
    <Link to={`/news/${item.newsId}`} className="news-hero">
      <div
        className="news-hero__image"
        style={{ background: cardGradient(index) }}
      >
        <Newspaper size={48} />
      </div>
      <div className="news-hero__overlay" />
      <div className="news-hero__body">
        <span className="news-hero__tag">Notícias</span>
        <h2 className="news-hero__title">{item.title}</h2>
        <p className="news-hero__date">{formatDate(item.publishedAt)}</p>
      </div>
    </Link>
  )
}

function NewsCard({ item, index }: { item: TorcedorNewsFeedItem; index: number }) {
  return (
    <Link to={`/news/${item.newsId}`} className="news-card">
      <div
        className="news-card__image"
        style={{ background: cardGradient(index) }}
      >
        <Newspaper size={24} />
      </div>
      <div className="news-card__body">
        <span className="news-card__date">{formatDate(item.publishedAt)}</span>
        <h3 className="news-card__title">{item.title}</h3>
        {item.summary ? (
          <p className="news-card__summary">{item.summary}</p>
        ) : null}
      </div>
    </Link>
  )
}

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
    return () => { cancelled = true }
  }, [])

  const hero = items[0]
  const featured = items.slice(1, 3)
  const rest = items.slice(3)

  return (
    <div className="news-root">
      <header className="news-header">
        <Link to="/" className="news-header__back" aria-label="Voltar ao início">
          <ArrowLeft size={18} />
        </Link>
        <h1 className="news-header__title">Notícias</h1>
        {!loading && total > 0 ? (
          <span className="news-header__count">{total}</span>
        ) : null}
      </header>

      <main className="news-content">
        {loading ? <NewsSkeletons /> : null}

        {error ? (
          <p className="news-error">{error}</p>
        ) : null}

        {!loading && !error && items.length === 0 ? (
          <p className="news-empty">Nenhuma notícia publicada no momento.</p>
        ) : null}

        {!loading && hero ? (
          <>
            {/* Desktop: hero + featured side-by-side; Mobile: hero stacked */}
            {featured.length > 0 ? (
              <div className="news-featured-area">
                <HeroCard item={hero} index={0} />
                <div className="news-featured-secondary">
                  {featured.map((item, i) => (
                    <NewsCard key={item.newsId} item={item} index={i + 1} />
                  ))}
                </div>
              </div>
            ) : (
              <HeroCard item={hero} index={0} />
            )}

            {rest.length > 0 ? (
              <nav className="news-grid" aria-label="Mais notícias">
                {rest.map((item, i) => (
                  <NewsCard key={item.newsId} item={item} index={i + 3} />
                ))}
              </nav>
            ) : null}
          </>
        ) : null}
      </main>

      <TorcedorBottomNav />
    </div>
  )
}
