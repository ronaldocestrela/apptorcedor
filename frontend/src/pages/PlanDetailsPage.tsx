import { useEffect, useMemo, useState } from 'react'
import { Link, useLocation, useParams } from 'react-router-dom'
import { ArrowLeft, Settings } from 'lucide-react'
import { isAxiosError } from 'axios'
import { plansService, type TorcedorPublishedPlanDetail } from '../features/plans/plansService'
import { subscriptionsService } from '../features/plans/subscriptionsService'
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
  const [subscribing, setSubscribing] = useState(false)
  const [subscribeError, setSubscribeError] = useState<string | null>(null)

  async function handleSubscribe() {
    if (!planId || subscribing)
      return
    try {
      setSubscribing(true)
      setSubscribeError(null)
      const res = await subscriptionsService.subscribe(planId, 'Card')
      const url = res.card?.checkoutUrl
      if (url) {
        window.location.href = url
        return
      }
      setSubscribeError('Não foi possível obter o link de pagamento. Tente novamente.')
      setSubscribing(false)
    }
    catch (e) {
      if (isAxiosError(e)) {
        if (e.response?.status === 409)
          setSubscribeError('Você já possui uma assinatura ativa.')
        else if (e.response?.status === 400)
          setSubscribeError('Este plano não está mais disponível.')
        else
          setSubscribeError(e.message ?? 'Erro ao iniciar o checkout.')
      }
      else {
        setSubscribeError(e instanceof Error ? e.message : 'Erro ao iniciar o checkout.')
      }
      setSubscribing(false)
    }
  }

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
      <div className="plans-figma-starfield" aria-hidden="true" />
      <header className="subpage-header subpage-header--tri plans-page__header">
        <Link to="/plans" className="subpage-header__back" aria-label="Voltar">
          <ArrowLeft size={24} strokeWidth={2} />
        </Link>
        <h1 className="subpage-header__title">Planos</h1>
        <Link to="/account" className="plans-page__settings-btn" aria-label="Configurações">
          <Settings size={24} strokeWidth={2} />
        </Link>
      </header>

      <main className="subpage-content plan-detail plan-detail--figma">
        {loading ? <p className="app-muted plan-detail__status">Carregando…</p> : null}
        {error ? <p role="alert" className="plan-detail__error">{error}</p> : null}

        {!loading && !error && plan ? (
          <div className="plan-detail__figma-stack">
            {isFeatured ? (
              <div className="plans-figma-chip-wrap">
                <span className="plans-figma-chip">Mais Popular</span>
              </div>
            ) : null}
            <div
              className={
                isFeatured
                  ? 'plan-detail__card plan-detail__card--featured'
                  : 'plan-detail__card'
              }
            >
              <div className="plan-detail__hero-block">
                <p className="plan-detail__name">{plan.name}</p>
                {plan.summary ? (
                  <p className="plan-detail__summary">{plan.summary}</p>
                ) : null}
                <p className="plan-detail__price">
                  <span className="plan-detail__price-currency">R$</span>
                  <span className="plan-detail__price-gap" aria-hidden="true" />
                  <span className="plan-detail__price-value">{formatPriceNumber(plan.price)}</span>
                  <span className="plan-detail__price-gap" aria-hidden="true" />
                  <span className="plan-detail__price-cycle">
                    {`/ ${billingCyclePeriodLabel(plan.billingCycle)}`}
                  </span>
                </p>
              </div>
              {sortedBenefits.length > 0 ? (
                <ul className="plan-detail__benefits">
                  {sortedBenefits.map((b, i) => (
                    <li
                      key={b.benefitId}
                      className={
                        i === 0
                          ? 'plan-detail__benefit-item plan-detail__benefit-item--lead'
                          : 'plan-detail__benefit-item'
                      }
                    >
                      {i > 0 ? <div className="plan-detail__benefit-rule" aria-hidden="true" /> : null}
                      <div className="plan-detail__benefit-copy">
                        <p className="plan-detail__benefit-title">{b.title}</p>
                        {b.description ? (
                          <p className="plan-detail__benefit-desc">{b.description}</p>
                        ) : null}
                      </div>
                    </li>
                  ))}
                </ul>
              ) : null}
              {subscribeError ? (
                <p role="alert" className="plan-detail__error plan-detail__error--inline">{subscribeError}</p>
              ) : null}
              <button
                type="button"
                className="plan-detail__cta"
                onClick={() => void handleSubscribe()}
                disabled={subscribing}
              >
                {subscribing ? 'AGUARDE…' : 'ASSINAR AGORA'}
              </button>
            </div>
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
