import { useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { isAxiosError } from 'axios'
import { plansService, type TorcedorPublishedPlanDetail } from '../features/plans/plansService'

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

export function PlanDetailsPage() {
  const { planId } = useParams<{ planId: string }>()
  const [plan, setPlan] = useState<TorcedorPublishedPlanDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

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
    <main style={{ maxWidth: 720, margin: '2rem auto', fontFamily: 'system-ui' }}>
      <p>
        <Link to="/plans">← Voltar aos planos</Link>
      </p>
      <h1>Detalhe do plano</h1>

      {loading ? <p>Carregando…</p> : null}
      {error ? <p style={{ color: '#721c24' }}>{error}</p> : null}

      {!loading && !error && plan ? (
        <article
          style={{
            border: '1px solid #ddd',
            borderRadius: 8,
            padding: '1.25rem',
            background: '#fafafa',
            marginTop: '1rem',
          }}
        >
          <h2 style={{ margin: '0 0 0.5rem', fontSize: '1.25rem' }}>{plan.name}</h2>
          <p style={{ margin: 0, fontSize: '1.35rem', fontWeight: 600 }}>
            {formatPrice(plan.price)}
            <span style={{ fontSize: '0.85rem', fontWeight: 400, color: '#555' }}>
              {' '}
              /
              {' '}
              {billingCycleLabel(plan.billingCycle)}
            </span>
          </p>
          {plan.discountPercentage > 0 && discountedPrice !== null ? (
            <div style={{ marginTop: '0.75rem' }}>
              <p style={{ margin: 0, fontSize: '0.9rem', color: '#2a6' }}>
                Desconto de
                {' '}
                {plan.discountPercentage}
                %
                {' '}
                — valor simulado:
                {' '}
                <strong>{formatPrice(discountedPrice)}</strong>
                {' '}
                por ciclo (após desconto).
              </p>
            </div>
          ) : null}
          {plan.summary ? (
            <p style={{ margin: '1rem 0 0', color: '#444' }}>{plan.summary}</p>
          ) : null}
          {plan.rulesNotes ? (
            <section style={{ marginTop: '1.25rem' }}>
              <h3 style={{ margin: '0 0 0.35rem', fontSize: '1rem' }}>Regras e condições</h3>
              <p style={{ margin: 0, whiteSpace: 'pre-wrap', color: '#333', fontSize: '0.95rem' }}>
                {plan.rulesNotes}
              </p>
            </section>
          ) : null}
          {plan.benefits.length > 0 ? (
            <section style={{ marginTop: '1.25rem' }}>
              <h3 style={{ margin: '0 0 0.35rem', fontSize: '1rem' }}>Benefícios inclusos</h3>
              <ul style={{ margin: 0, paddingLeft: '1.1rem', color: '#333' }}>
                {plan.benefits.map(b => (
                  <li key={b.benefitId} style={{ marginBottom: '0.35rem' }}>
                    <strong>{b.title}</strong>
                    {b.description ? (
                      <>
                        {' '}
                        —
                        {' '}
                        {b.description}
                      </>
                    ) : null}
                  </li>
                ))}
              </ul>
            </section>
          ) : null}
          <p style={{ margin: '1.5rem 0 0' }}>
            <button
              type="button"
              disabled
              title="A contratação online será habilitada nas partes D.3 e D.4 do roadmap."
              style={{
                padding: '0.5rem 1rem',
                borderRadius: 6,
                border: '1px solid #ccc',
                background: '#eee',
                cursor: 'not-allowed',
                width: '100%',
              }}
            >
              Contratar
            </button>
          </p>
          <p style={{ margin: '0.5rem 0 0', fontSize: '0.85rem', color: '#666' }}>
            O checkout e pagamento serão disponibilizados nas próximas etapas do projeto.
          </p>
        </article>
      ) : null}
    </main>
  )
}
