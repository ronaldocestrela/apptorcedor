import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getApiErrorMessage } from '../../shared/auth'
import { listSaaSPlans, listTenants } from '../../shared/backoffice'

export function BackofficeDashboardPage() {
  const [tenantTotal, setTenantTotal] = useState<number | null>(null)
  const [planTotal, setPlanTotal] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    async function load() {
      setError(null)
      setLoading(true)
      try {
        const [t, p] = await Promise.all([listTenants({ page: 1, pageSize: 1 }), listSaaSPlans(1, 1)])
        if (!cancelled) {
          setTenantTotal(t.totalCount)
          setPlanTotal(p.totalCount)
        }
      } catch (e: unknown) {
        if (!cancelled) {
          setError(getApiErrorMessage(e, 'Não foi possível carregar os totais.'))
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    }
    void load()
    return () => {
      cancelled = true
    }
  }, [])

  return (
    <section className="bo-page">
      <h1 className="bo-page__title">Painel</h1>
      <p className="bo-page__lead">Gestão central da plataforma (master DB). Rotas <code>/api/backoffice/*</code>.</p>
      {error ? (
        <p className="billing-page__error" role="alert">
          {error}
        </p>
      ) : null}
      {loading ? <p className="bo-muted">Carregando…</p> : null}
      <div className="bo-cards">
        <Link to="/backoffice/tenants" className="bo-card">
          <span className="bo-card__label">Tenants</span>
          <strong className="bo-card__value">{tenantTotal ?? '—'}</strong>
          <span className="bo-card__hint">Clubes cadastrados</span>
        </Link>
        <Link to="/backoffice/plans" className="bo-card">
          <span className="bo-card__label">Planos SaaS</span>
          <strong className="bo-card__value">{planTotal ?? '—'}</strong>
          <span className="bo-card__hint">Ofertas da plataforma</span>
        </Link>
        <Link to="/backoffice/tenant-plans" className="bo-card">
          <span className="bo-card__label">Vínculos</span>
          <strong className="bo-card__value">→</strong>
          <span className="bo-card__hint">Plano atribuído a cada tenant</span>
        </Link>
      </div>
    </section>
  )
}
