import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeft, Settings } from 'lucide-react'
import { plansService, type TorcedorPublishedPlan } from '../features/plans/plansService'
import { DEFAULT_DOCUMENT_TITLE } from '../shared/seo'
import { TorcedorBottomNav } from '../shared/torcedorBottomNav'
import './AppShell.css'

function formatPriceNumber(value: number): string {
  return value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

/** Período de cobrança após o preço (ex.: `/ mês`, `/ ano`), conforme `billingCycle` do plano. */
function billingCyclePeriodLabel(cycle: string): string {
  switch (cycle) {
    case 'Monthly':
      return 'mês'
    case 'Yearly':
      return 'ano'
    case 'Quarterly':
      return 'trimestre'
    default:
      return cycle
  }
}

export function PlansPage() {
  const [plans, setPlans] = useState<TorcedorPublishedPlan[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        setLoading(true)
        const catalog = await plansService.listPublished()
        if (!cancelled) {
          setPlans(catalog.items)
          setError(null)
        }
      }
      catch (e) {
        if (!cancelled)
          setError(e instanceof Error ? e.message : 'Erro ao carregar planos')
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
    document.title = 'Planos de Sócio | FFC'
    return () => {
      document.title = DEFAULT_DOCUMENT_TITLE
    }
  }, [])

  return (
    <div className="plans-root">
      <div className="plans-figma-starfield" aria-hidden="true" />
      <header className="subpage-header subpage-header--tri plans-page__header">
        <Link to="/" className="subpage-header__back" aria-label="Voltar">
          <ArrowLeft size={24} strokeWidth={2} />
        </Link>
        <h1 className="subpage-header__title">Planos</h1>
        <Link to="/account" className="plans-page__settings-btn" aria-label="Configurações">
          <Settings size={24} strokeWidth={2} />
        </Link>
      </header>

      <main className="subpage-content plans-page plans-page--figma">
        {loading ? <p className="app-muted">Carregando…</p> : null}
        {error ? <p role="alert" style={{ color: '#ffc6c6', fontSize: '0.9rem' }}>{error}</p> : null}
        {!loading && !error && plans.length === 0 ? (
          <p className="app-muted">Nenhum plano publicado no momento.</p>
        ) : null}

        {!loading && !error && plans.length > 0 ? (
          <ul className="plans-figma-list" aria-label="Lista de planos">
            {plans.map((plan, index) => {
              const featured = index === 0
              return (
                <li key={plan.planId} className="plans-figma-stack">
                  {featured ? (
                    <div className="plans-figma-chip-wrap">
                      <span className="plans-figma-chip">Mais Popular</span>
                    </div>
                  ) : null}
                  <div
                    className={
                      featured
                        ? 'plans-figma-card plans-figma-card--featured'
                        : 'plans-figma-card'
                    }
                  >
                    <p className="plans-figma-card__title">{plan.name}</p>
                    {plan.summary ? (
                      <p className="plans-figma-card__summary">{plan.summary}</p>
                    ) : null}
                    <p className="plans-figma-card__price">
                      <span className="plans-figma-card__price-currency">R$</span>
                      <span className="plans-figma-card__price-gap" aria-hidden="true" />
                      <span className="plans-figma-card__price-value">{formatPriceNumber(plan.price)}</span>
                      <span className="plans-figma-card__price-gap" aria-hidden="true" />
                      <span className="plans-figma-card__cycle">
                        {`/ ${billingCyclePeriodLabel(plan.billingCycle)}`}
                      </span>
                    </p>
                    {plan.discountPercentage > 0 ? (
                      <p className="plans-figma-card__discount">
                        Desconto
                        {' '}
                        {plan.discountPercentage}
                        %
                      </p>
                    ) : null}
                    <Link
                      to={`/plans/${plan.planId}`}
                      state={{ featured }}
                      className="plans-figma-card__cta"
                    >
                      MAIS DETALHES
                    </Link>
                  </div>
                </li>
              )
            })}
          </ul>
        ) : null}
      </main>
      <TorcedorBottomNav />
    </div>
  )
}
