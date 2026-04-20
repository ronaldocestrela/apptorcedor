import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { plansService, type TorcedorPublishedPlan } from '../features/plans/plansService'
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
    <main className="app-shell plans-page">
      <p>
        <Link to="/" className="app-back-link">← Início</Link>
      </p>
      <section className="app-surface">
        <h1 className="app-title">Planos de sócio</h1>
        <p className="app-muted">
          Confira os planos disponíveis. Toque em Assinar para ver o detalhe e seguir para o checkout (PIX ou cartão).
        </p>
        {loading ? <p className="app-muted">Carregando…</p> : null}
        {error ? <p style={{ color: '#ffc6c6' }}>{error}</p> : null}
        {!loading && !error && plans.length === 0 ? (
          <p className="app-muted">Nenhum plano publicado no momento.</p>
        ) : null}
      </section>

      <section className="plans-page__grid" aria-label="Lista de planos">
        {plans.map(plan => (
          <article
            key={plan.planId}
            className="plans-page__card"
          >
            <h2 style={{ margin: '0 0 0.5rem', fontSize: '1.15rem' }}>{plan.name}</h2>
            <p className="plans-page__price">
              {formatPrice(plan.price)}
              <span className="plans-page__cycle">
                {' '}
                /
                {' '}
                {billingCycleLabel(plan.billingCycle)}
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
            {plan.summary ? <p className="app-muted" style={{ marginTop: '0.75rem' }}>{plan.summary}</p> : null}
            {plan.benefits.length > 0 ? (
              <div style={{ marginTop: '0.75rem' }}>
                <strong style={{ fontSize: '0.85rem' }}>Benefícios</strong>
                <ul style={{ margin: '0.35rem 0 0', paddingLeft: '1.1rem', fontSize: '0.9rem', color: '#cce9d4' }}>
                  {plan.benefits.slice(0, 5).map(b => (
                    <li key={b.benefitId}>{b.title}</li>
                  ))}
                </ul>
                {plan.benefits.length > 5 ? (
                  <p className="app-muted" style={{ margin: '0.25rem 0 0', fontSize: '0.8rem' }}>
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
                className="dashboard-page__link-card"
              >
                Assinar
              </Link>
            </p>
          </article>
        ))}
      </section>
    </main>
  )
}
