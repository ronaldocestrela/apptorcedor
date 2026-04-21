import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeft, Gift, Settings } from 'lucide-react'
import { listEligibleBenefitOffers, type TorcedorEligibleBenefitOffer } from '../features/torcedor/torcedorBenefitsApi'
import { DEFAULT_DOCUMENT_TITLE } from '../shared/seo'
import { TorcedorBottomNav } from '../shared/torcedorBottomNav'
import './AppShell.css'

function formatValidUntil(endAt: string) {
  return `Válido até ${new Date(endAt).toLocaleDateString('pt-BR')}*`
}

export function BenefitsEligiblePage() {
  const [items, setItems] = useState<TorcedorEligibleBenefitOffer[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        setLoading(true)
        const page = await listEligibleBenefitOffers({ pageSize: 50 })
        if (!cancelled) {
          setItems(page.items)
          setError(null)
        }
      }
      catch (e) {
        if (!cancelled)
          setError(e instanceof Error ? e.message : 'Erro ao carregar benefícios')
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

  useEffect(() => {
    document.title = 'Benefícios | FFC'
    return () => {
      document.title = DEFAULT_DOCUMENT_TITLE
    }
  }, [])

  return (
    <div className="benefits-root">
      <header className="subpage-header subpage-header--tri benefits-page__header">
        <Link to="/" className="subpage-header__back" aria-label="Voltar">
          <ArrowLeft size={24} strokeWidth={2} aria-hidden="true" />
        </Link>
        <h1 className="subpage-header__title">Benefícios</h1>
        <Link
          to="/account"
          className="subpage-header__badge-btn"
          aria-label="Conta e configurações"
        >
          <Settings size={24} strokeWidth={2} aria-hidden="true" />
        </Link>
      </header>

      <main className="subpage-content benefits-page">
        {loading ? <p className="app-muted">Carregando…</p> : null}
        {error ? (
          <p role="alert" className="benefits-page__error">
            {error}
          </p>
        ) : null}
        {!loading && items.length === 0 && !error ? (
          <p className="benefits-page__empty">Nenhuma oferta elegível no momento.</p>
        ) : null}
        <ul className="benefits-figma-list">
          {items.map((item) => (
            <li key={item.offerId}>
              <article className="benefits-figma-card">
                <div className="benefits-figma-card__top">
                  <div className="benefits-figma-card__icon-wrap" aria-hidden="true">
                    <Gift size={30} stroke="#8cd392" strokeWidth={2} />
                  </div>
                  <div className="benefits-figma-card__copy">
                    <p className="benefits-figma-card__eyebrow">{item.partnerName}</p>
                    <p className="benefits-figma-card__headline">{item.title}</p>
                    <p className="benefits-figma-card__valid">{formatValidUntil(item.endAt)}</p>
                  </div>
                </div>
                <div className="benefits-figma-card__cta-row">
                  <Link
                    to={`/benefits/${item.offerId}`}
                    className="benefits-figma-card__cta"
                  >
                    Resgatar Benefício
                  </Link>
                </div>
              </article>
            </li>
          ))}
        </ul>
      </main>

      <TorcedorBottomNav />
    </div>
  )
}
