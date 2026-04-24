import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { isAxiosError } from 'axios'
import { cardInstallmentsCheckoutShortHint, planOffersCardInstallmentsAtCheckout } from '../features/plans/cardInstallmentsCopy'
import { plansService, type TorcedorPublishedPlanDetail } from '../features/plans/plansService'
import { subscriptionsService, type SubscriptionPaymentMethod } from '../features/plans/subscriptionsService'
import './AppShell.css'

function billingCycleLabel(cycle: string): string {
  switch (cycle) {
    case 'Monthly':
      return 'Mensal'
    case 'Yearly':
      return 'Anual'
    case 'Quarterly':
      return 'Trimestral'
    default:
      return cycle
  }
}

function formatPrice(value: number): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

export function SubscriptionCheckoutPage() {
  const navigate = useNavigate()
  const { planId } = useParams<{ planId: string }>()
  const [plan, setPlan] = useState<TorcedorPublishedPlanDetail | null>(null)
  const [method, setMethod] = useState<SubscriptionPaymentMethod>('Card')
  const [loadingPlan, setLoadingPlan] = useState(true)
  const [planError, setPlanError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)

  const discountedPrice = useMemo(() => {
    if (!plan)
      return null
    const pct = plan.discountPercentage
    if (pct <= 0)
      return plan.price
    return Math.round(plan.price * (1 - pct / 100) * 100) / 100
  }, [plan])

  useEffect(() => {
    if (!planId) {
      setLoadingPlan(false)
      setPlanError('Plano inválido.')
      return
    }
    let cancelled = false
    void (async () => {
      try {
        setLoadingPlan(true)
        const detail = await plansService.getById(planId)
        if (!cancelled) {
          setPlan(detail)
          setPlanError(null)
        }
      }
      catch (e) {
        if (!cancelled) {
          if (isAxiosError(e) && e.response?.status === 404) {
            setPlan(null)
            setPlanError('Plano não encontrado ou não está mais disponível.')
          }
          else {
            setPlan(null)
            setPlanError(e instanceof Error ? e.message : 'Erro ao carregar o plano')
          }
        }
      }
      finally {
        if (!cancelled)
          setLoadingPlan(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [planId])

  async function onConfirm(e: React.FormEvent) {
    e.preventDefault()
    if (!planId)
      return
    setSubmitError(null)
    setSubmitting(true)
    try {
      const res = await subscriptionsService.subscribe(planId, method)
      if (!plan)
        return
      navigate('/subscription/confirmation', {
        replace: true,
        state: { checkout: res, planName: plan.name, billingCycle: plan.billingCycle },
      })
    }
    catch (err) {
      if (isAxiosError(err)) {
        const status = err.response?.status
        const errObj = err.response?.data as { error?: string } | undefined
        if (status === 409)
          setSubmitError('Não é possível contratar agora (assinatura ativa ou pagamento pendente).')
        else if (status === 400)
          setSubmitError('Não foi possível iniciar a contratação. Verifique seu cadastro ou tente outro plano.')
        else if (errObj?.error === 'plan_not_available')
          setSubmitError('Plano indisponível.')
        else
          setSubmitError(err.message)
      }
      else {
        setSubmitError(err instanceof Error ? err.message : 'Erro ao contratar')
      }
    }
    finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="subscription-checkout-root">
      <main className="subscription-checkout-page">
        <Link className="subscription-checkout-page__back" to={planId ? `/plans/${planId}` : '/plans'}>
          ← Voltar ao plano
        </Link>
        <h1 className="subscription-checkout-page__title">Checkout</h1>

        {loadingPlan ? <p className="subscription-checkout-page__muted">Carregando…</p> : null}
        {planError ? <p className="subscription-checkout-page__error" role="alert">{planError}</p> : null}

        {!loadingPlan && !planError && plan ? (
          <section className="subscription-checkout-page__card">
            <h2 className="subscription-checkout-page__plan-name">{plan.name}</h2>
            <p className="subscription-checkout-page__price">
              {formatPrice(plan.price)}
              <span className="subscription-checkout-page__price-cycle">
                {' '}
                /
                {' '}
                {billingCycleLabel(plan.billingCycle)}
              </span>
            </p>
            {plan.discountPercentage > 0 && discountedPrice !== null ? (
              <p className="subscription-checkout-page__discount">
                Valor com desconto (cobrança inicial):
                {' '}
                <strong>{formatPrice(discountedPrice)}</strong>
              </p>
            ) : null}
            {planOffersCardInstallmentsAtCheckout(plan.billingCycle) ? (
              <p className="subscription-checkout-page__installments-note subscription-checkout-page__installments-note--prominent">
                {cardInstallmentsCheckoutShortHint}
              </p>
            ) : null}

            <form onSubmit={e => void onConfirm(e)}>
              <fieldset className="subscription-checkout-page__fieldset">
                <legend className="subscription-checkout-page__legend">Forma de pagamento</legend>
                <label className="subscription-checkout-page__radio">
                  <input
                    type="radio"
                    name="pay"
                    checked={method === 'Pix'}
                    onChange={() => setMethod('Pix')}
                  />
                  {' '}
                  PIX
                </label>
                <label className="subscription-checkout-page__radio">
                  <input
                    type="radio"
                    name="pay"
                    checked={method === 'Card'}
                    onChange={() => setMethod('Card')}
                  />
                  {' '}
                  Cartão (checkout externo)
                </label>
              </fieldset>
              {submitError ? <p className="subscription-checkout-page__submit-error" role="alert">{submitError}</p> : null}
              <button
                type="submit"
                className="subscription-checkout-page__submit"
                disabled={submitting}
              >
                {submitting ? 'Confirmando…' : 'Confirmar contratação'}
              </button>
            </form>
          </section>
        ) : null}
      </main>
    </div>
  )
}
