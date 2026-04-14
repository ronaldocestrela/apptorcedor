import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { isAxiosError } from 'axios'
import { plansService, type TorcedorPublishedPlanDetail } from '../features/plans/plansService'
import { subscriptionsService, type SubscriptionPaymentMethod } from '../features/plans/subscriptionsService'

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
  const [method, setMethod] = useState<SubscriptionPaymentMethod>('Pix')
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
    <main style={{ maxWidth: 720, margin: '2rem auto', fontFamily: 'system-ui' }}>
      <p>
        <Link to={planId ? `/plans/${planId}` : '/plans'}>← Voltar ao plano</Link>
      </p>
      <h1>Checkout</h1>

      {loadingPlan ? <p>Carregando…</p> : null}
      {planError ? <p style={{ color: '#721c24' }}>{planError}</p> : null}

      {!loadingPlan && !planError && plan ? (
        <section
          style={{
            border: '1px solid #ddd',
            borderRadius: 8,
            padding: '1.25rem',
            background: '#fafafa',
            marginTop: '1rem',
          }}
        >
          <h2 style={{ margin: '0 0 0.5rem', fontSize: '1.15rem' }}>{plan.name}</h2>
          <p style={{ margin: 0, fontSize: '1.1rem' }}>
            {formatPrice(plan.price)}
            <span style={{ fontSize: '0.85rem', color: '#555' }}>
              {' '}
              /
              {' '}
              {billingCycleLabel(plan.billingCycle)}
            </span>
          </p>
          {plan.discountPercentage > 0 && discountedPrice !== null ? (
            <p style={{ margin: '0.5rem 0 0', fontSize: '0.9rem', color: '#2a6' }}>
              Valor com desconto (cobrança inicial):
              {' '}
              <strong>{formatPrice(discountedPrice)}</strong>
            </p>
          ) : null}

          <form onSubmit={e => void onConfirm(e)} style={{ marginTop: '1.25rem' }}>
            <fieldset style={{ border: 'none', padding: 0, margin: 0 }}>
              <legend style={{ fontWeight: 600, marginBottom: 8 }}>Forma de pagamento</legend>
              <label style={{ display: 'block', marginBottom: 8 }}>
                <input
                  type="radio"
                  name="pay"
                  checked={method === 'Pix'}
                  onChange={() => setMethod('Pix')}
                />
                {' '}
                PIX
              </label>
              <label style={{ display: 'block', marginBottom: 8 }}>
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
            {submitError ? <p style={{ color: '#721c24' }}>{submitError}</p> : null}
            <button
              type="submit"
              disabled={submitting}
              style={{
                marginTop: '1rem',
                padding: '0.5rem 1rem',
                borderRadius: 6,
                border: '1px solid #1976d2',
                background: '#1976d2',
                color: '#fff',
                cursor: submitting ? 'wait' : 'pointer',
                width: '100%',
              }}
            >
              {submitting ? 'Confirmando…' : 'Confirmar contratação'}
            </button>
          </form>
        </section>
      ) : null}
    </main>
  )
}
