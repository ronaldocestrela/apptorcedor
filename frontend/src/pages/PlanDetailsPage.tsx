import { useEffect, useMemo, useState } from 'react'
import { Link, useLocation, useParams } from 'react-router-dom'
import { ArrowLeft, Settings } from 'lucide-react'
import { isAxiosError } from 'axios'
import { plansService, type TorcedorPublishedPlanDetail } from '../features/plans/plansService'
import { TorcedorBottomNav } from '../shared/torcedorBottomNav'
import './AppShell.css'

function formatPriceNumber(value: number): string {
  return value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

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

export function PlanDetailsPage() {
  const { planId } = useParams<{ planId: string }>()
  const location = useLocation()
  const isFeatured = (location.state as { featured?: boolean } | null)?.featured === true
  const [plan, setPlan] = useState<TorcedorPublishedPlanDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const sortedBenefits = useMemo(() => {
    if (!plan)
      return []
    return [...plan.benefits].sort((a, b) => a.sortOrder - b.sortOrder)
  }, [plan])

  useEffect(() => {
    if (!planId) {
      setLoading(false)
      setError('Plano inválido.')
      return
    }

    let cancelled = false
    void (async () => {
      try {
        setLoading(true)
        const detail = await plansService.getById(planId)
        if (!cancelled) {
          setPlan(detail)
          setError(null)
        }
      }
      catch (e) {
        if (cancelled)
          return
        if (isAxiosError(e) && e.response?.status === 404) {
          setPlan(null)
          setError('Plano não encontrado ou não está mais disponível.')
        }
        else {
          setPlan(null)
          setError(e instanceof Error ? e.message : 'Erro ao carregar o plano')
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
  }, [planId])

  return (
    <div className="plans-root">
      <header className="subpage-header">
        <Link to="/plans" className="subpage-header__back" aria-label="Voltar">
          <ArrowLeft size={18} />
        </Link>
        <h1 className="subpage-header__title plans-page__header-title">Planos</h1>
        <Link to="/account" className="plans-page__settings-btn" aria-label="Configurações">
          <Settings size={20} stroke="currentColor" />
        </Link>
      </header>

      <main className="subpage-content plan-detail">
        {loading ? <p className="app-muted">Carregando…</p> : null}
        {error ? <p role="alert" className="plan-detail__error">{error}</p> : null}

        {!loading && !error && plan ? (
          <div className="plan-detail__card">
            {isFeatured ? (
              <span className="plans-page__badge">Mais Popular</span>
            ) : null}
            <p className="plan-detail__name">{plan.name}</p>
            {plan.summary ? (
              <p className="plan-detail__summary">{plan.summary}</p>
            ) : null}
            <p className="plans-page__price">
              <span className="plans-page__price-currency">R$</span>
              <span className="plans-page__price-value">{formatPriceNumber(plan.price)}</span>
              <span className="plans-page__price-cycle">
                {`/ ${billingCyclePeriodLabel(plan.billingCycle)}`}
              </span>
            </p>
            {sortedBenefits.length > 0 ? (
              <ul className="plan-detail__benefits">
                {sortedBenefits.map(b => (
                  <li key={b.benefitId} className="plan-detail__benefit-item">
                    {b.title}
                    {b.description ? ` — ${b.description}` : ''}
                  </li>
                ))}
              </ul>
            ) : null}
            <Link to={`/plans/${plan.planId}/checkout`} className="plan-detail__cta">
              Assinar agora
            </Link>
            <p className="plan-detail__note">
              Você será direcionado ao checkout em uma plataforma externa.*
            </p>
          </div>
        ) : null}
      </main>
      <TorcedorBottomNav />
    </div>
  )
}
