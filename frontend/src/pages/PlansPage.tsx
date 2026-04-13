import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { plansService, type TorcedorPublishedPlan } from '../features/plans/plansService'

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
    <main style={{ maxWidth: 720, margin: '2rem auto', fontFamily: 'system-ui' }}>
      <p>
        <Link to="/">← Início</Link>
      </p>
      <h1>Planos de sócio</h1>
      <p style={{ color: '#555' }}>
        Confira os planos disponíveis. Toque em Assinar para ver o detalhe completo; a contratação online será habilitada nas partes D.3 e D.4.
      </p>
      {loading ? <p>Carregando…</p> : null}
      {error ? <p style={{ color: '#721c24' }}>{error}</p> : null}
      {!loading && !error && plans.length === 0 ? (
        <p>Nenhum plano publicado no momento.</p>
      ) : null}
      <div
        style={{
          display: 'grid',
          gap: '1.25rem',
          gridTemplateColumns: 'repeat(auto-fill, minmax(260px, 1fr))',
          marginTop: '1.5rem',
        }}
      >
        {plans.map(plan => (
          <article
            key={plan.planId}
            style={{
              border: '1px solid #ddd',
              borderRadius: 8,
              padding: '1.25rem',
              background: '#fafafa',
            }}
          >
            <h2 style={{ margin: '0 0 0.5rem', fontSize: '1.15rem' }}>{plan.name}</h2>
            <p style={{ margin: 0, fontSize: '1.35rem', fontWeight: 600 }}>
              {formatPrice(plan.price)}
              <span style={{ fontSize: '0.85rem', fontWeight: 400, color: '#555' }}>
                {' '}
                /
                {' '}
                {billingCycleLabel(plan.billingCycle)}
              </span>
            </p>
            {plan.discountPercentage > 0 ? (
              <p style={{ margin: '0.35rem 0 0', fontSize: '0.9rem', color: '#2a6' }}>
                Desconto
                {' '}
                {plan.discountPercentage}
                %
              </p>
            ) : null}
            {plan.summary ? <p style={{ margin: '0.75rem 0 0', color: '#444' }}>{plan.summary}</p> : null}
            {plan.benefits.length > 0 ? (
              <div style={{ marginTop: '0.75rem' }}>
                <strong style={{ fontSize: '0.85rem' }}>Benefícios</strong>
                <ul style={{ margin: '0.35rem 0 0', paddingLeft: '1.1rem', fontSize: '0.9rem', color: '#333' }}>
                  {plan.benefits.slice(0, 5).map(b => (
                    <li key={b.benefitId}>{b.title}</li>
                  ))}
                </ul>
                {plan.benefits.length > 5 ? (
                  <p style={{ margin: '0.25rem 0 0', fontSize: '0.8rem', color: '#666' }}>
                    +
                    {plan.benefits.length - 5}
                    {' '}
                    outros
                  </p>
                ) : null}
              </div>
            ) : null}
            <p style={{ margin: '1rem 0 0' }}>
              <Link
                to={`/plans/${plan.planId}`}
                style={{
                  display: 'block',
                  textAlign: 'center',
                  padding: '0.5rem 1rem',
                  borderRadius: 6,
                  border: '1px solid #1976d2',
                  background: '#1976d2',
                  color: '#fff',
                  textDecoration: 'none',
                  width: '100%',
                  boxSizing: 'border-box',
                }}
              >
                Assinar
              </Link>
            </p>
          </article>
        ))}
      </div>
    </main>
  )
}
