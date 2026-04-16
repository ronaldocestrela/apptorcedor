import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import { listEligibleBenefitOffers, type TorcedorEligibleBenefitOffer } from '../features/torcedor/torcedorBenefitsApi'
import { TorcedorBottomNav } from '../shared/torcedorBottomNav'
import './AppShell.css'

export function BenefitsEligiblePage() {
  const [items, setItems] = useState<TorcedorEligibleBenefitOffer[]>([])
  const [total, setTotal] = useState(0)
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
          setTotal(page.totalCount)
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

  return (
    <div className="benefits-root">
      <header className="subpage-header">
        <Link to="/" className="subpage-header__back" aria-label="Voltar">
          <ArrowLeft size={18} />
        </Link>
        <h1 className="subpage-header__title">Benefícios</h1>
        {!loading && !error ? (
          <span className="subpage-header__badge">{total}</span>
        ) : null}
      </header>

      <main className="subpage-content">
        <p className="benefits-intro">
          Ofertas ativas e vigentes alinhadas ao seu plano e status de sócio.
        </p>
        {loading ? <p className="app-muted">Carregando…</p> : null}
        {error ? (
          <p role="alert" style={{ color: '#ffc6c6', fontSize: '0.9rem' }}>
            {error}
          </p>
        ) : null}
        {!loading && items.length === 0 && !error ? (
          <p className="benefits-empty">Nenhuma oferta elegível no momento.</p>
        ) : null}
        <ul className="benefit-offer-list">
          {items.map(item => (
            <li key={item.offerId}>
              <Link to={`/benefits/${item.offerId}`} className="benefit-offer-card benefit-offer-card--link">
                <p className="benefit-offer-card__title">{item.title}</p>
                <span className="benefit-offer-card__partner">{item.partnerName}</span>
                {item.description ? (
                  <p className="benefit-offer-card__description">{item.description}</p>
                ) : null}
                <p className="benefit-offer-card__dates">
                  Válido de
                  {' '}
                  {new Date(item.startAt).toLocaleDateString('pt-BR')}
                  {' '}
                  até
                  {' '}
                  {new Date(item.endAt).toLocaleDateString('pt-BR')}
                </p>
              </Link>
            </li>
          ))}
        </ul>
      </main>

      <TorcedorBottomNav />
    </div>
  )
}
