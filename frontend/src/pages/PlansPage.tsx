import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeft, Settings } from 'lucide-react'
import { plansService, type TorcedorPublishedPlan } from '../features/plans/plansService'
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

  return (
    <div className="plans-root">
      <header className="subpage-header">
        <Link to="/" className="subpage-header__back" aria-label="Voltar">
          <ArrowLeft size={18} />
        </Link>
        <h1 className="subpage-header__title plans-page__header-title">Planos</h1>
        <Link to="/account" className="plans-page__settings-btn" aria-label="Configurações">
          <Settings size={20} stroke="currentColor" />
        </Link>
      </header>

      <main className="subpage-content plans-page">
        {loading ? <p className="app-muted">Carregando…</p> : null}
        {error ? <p role="alert" style={{ color: '#ffc6c6', fontSize: '0.9rem' }}>{error}</p> : null}
        {!loading && !error && plans.length === 0 ? (
          <p className="app-muted">Nenhum plano publicado no momento.</p>
        ) : null}

        {!loading && !error && plans.length > 0 ? (
          <ul className="plans-page__list" aria-label="Lista de planos">
            {plans.map((plan, index) => (
              <li key={plan.planId} className="plans-page__item">
                {index === 0 ? (
                  <span className="plans-page__badge">Mais Popular</span>
                ) : null}
                <div
                  className={
                    index === 0
                      ? 'plans-page__card plans-page__card--featured'
                      : 'plans-page__card'
                  }
                >
                  <p className="plans-page__name">{plan.name}</p>
                  {plan.summary ? (
                    <p className="plans-page__summary">{plan.summary}</p>
                  ) : null}
                  <p className="plans-page__price">
                    <span className="plans-page__price-currency">R$</span>
                    <span className="plans-page__price-value">{formatPriceNumber(plan.price)}</span>
                    <span className="plans-page__price-cycle">
                      {`/ ${billingCyclePeriodLabel(plan.billingCycle)}`}
                    </span>
                  </p>
                  {plan.discountPercentage > 0 ? (
                    <p className="plans-page__discount">
                      Desconto
                      {' '}
                      {plan.discountPercentage}
                      %
                    </p>
                  ) : null}
                  <Link to={`/plans/${plan.planId}`} className="plans-page__cta">
                    Mais detalhes
                  </Link>
                </div>
              </li>
            ))}
          </ul>
        ) : null}
      </main>
      <TorcedorBottomNav />
    </div>
  )
}
