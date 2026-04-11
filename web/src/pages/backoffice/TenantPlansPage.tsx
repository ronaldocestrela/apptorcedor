import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { getApiErrorMessage } from '../../shared/auth'
import {
  assignPlanToTenant,
  listSaaSPlans,
  listTenants,
  listTenantsByPlan,
  type SaaSPlanDto,
  type TenantListItemDto,
  type TenantPlanSummaryDto,
} from '../../shared/backoffice'
import { BillingCycle } from '../../shared/backoffice/types'
import { formatTenantPlanStatus } from '../../shared/backoffice/formatters'

const PAGE_SIZE = 10

export function TenantPlansPage() {
  const [tenants, setTenants] = useState<TenantListItemDto[]>([])
  const [plans, setPlans] = useState<SaaSPlanDto[]>([])
  const [selectedTenantId, setSelectedTenantId] = useState('')
  const [selectedPlanId, setSelectedPlanId] = useState('')
  const [assignStart, setAssignStart] = useState(() => new Date().toISOString().slice(0, 10))
  const [assignEnd, setAssignEnd] = useState('')
  const [assignCycle, setAssignCycle] = useState<BillingCycle>(BillingCycle.Monthly)
  const [assignBusy, setAssignBusy] = useState(false)
  const [assignError, setAssignError] = useState<string | null>(null)
  const [assignOk, setAssignOk] = useState<string | null>(null)

  const [byPlanItems, setByPlanItems] = useState<TenantPlanSummaryDto[]>([])
  const [byPlanPage, setByPlanPage] = useState(1)
  const [byPlanTotal, setByPlanTotal] = useState(0)
  const [byPlanLoading, setByPlanLoading] = useState(false)
  const [byPlanError, setByPlanError] = useState<string | null>(null)

  const loadLookups = useCallback(async () => {
    try {
      const [tRes, pRes] = await Promise.all([
        listTenants({ page: 1, pageSize: 200, search: null, status: null }),
        listSaaSPlans(1, 200),
      ])
      setTenants(tRes.items)
      setPlans(pRes.items)
      setSelectedTenantId((prev) => prev || tRes.items[0]?.id || '')
      setSelectedPlanId((prev) => prev || pRes.items[0]?.id || '')
    } catch (e: unknown) {
      setAssignError(getApiErrorMessage(e, 'Falha ao carregar listas.'))
    }
  }, [])

  useEffect(() => {
    void loadLookups()
  }, [loadLookups])

  const loadByPlan = useCallback(async (planId: string, page: number) => {
    if (!planId) return
    setByPlanError(null)
    setByPlanLoading(true)
    try {
      const r = await listTenantsByPlan(planId, page, PAGE_SIZE)
      setByPlanItems(r.items)
      setByPlanTotal(r.totalCount)
      setByPlanPage(r.page)
    } catch (e: unknown) {
      setByPlanError(getApiErrorMessage(e, 'Falha ao listar tenants do plano.'))
      setByPlanItems([])
    } finally {
      setByPlanLoading(false)
    }
  }, [])

  useEffect(() => {
    if (selectedPlanId) {
      void loadByPlan(selectedPlanId, 1)
    }
  }, [selectedPlanId, loadByPlan])

  async function onAssign(e: FormEvent) {
    e.preventDefault()
    setAssignError(null)
    setAssignOk(null)
    if (!selectedTenantId || !selectedPlanId) {
      setAssignError('Selecione tenant e plano.')
      return
    }
    setAssignBusy(true)
    try {
      const start = new Date(assignStart + 'T12:00:00.000Z').toISOString()
      const end = assignEnd.trim() === '' ? null : new Date(assignEnd + 'T12:00:00.000Z').toISOString()
      await assignPlanToTenant({
        tenantId: selectedTenantId,
        saaSPlanId: selectedPlanId,
        startDate: start,
        endDate: end,
        billingCycle: assignCycle,
      })
      setAssignOk('Plano atribuído.')
      if (selectedPlanId) void loadByPlan(selectedPlanId, byPlanPage)
    } catch (err: unknown) {
      setAssignError(getApiErrorMessage(err, 'Falha ao atribuir plano.'))
    } finally {
      setAssignBusy(false)
    }
  }

  const totalPages = Math.max(1, Math.ceil(byPlanTotal / PAGE_SIZE))

  return (
    <section className="bo-page">
      <h1 className="bo-page__title">Vínculos tenant ↔ plano SaaS</h1>
      <p className="bo-muted">Atribua um plano SaaS a um tenant. Para revogar, use a aba Plano na página do tenant.</p>

      <div className="bo-panel">
        <h2 className="bo-subtitle">Nova atribuição</h2>
        {assignError ? (
          <p className="billing-page__error" role="alert">
            {assignError}
          </p>
        ) : null}
        {assignOk ? (
          <p className="bo-hint" role="status">
            {assignOk}
          </p>
        ) : null}
        <form className="bo-form-grid" onSubmit={onAssign}>
          <label className="billing-page__field">
            Tenant
            <select
              className="auth-field__input"
              value={selectedTenantId}
              onChange={(e) => setSelectedTenantId(e.target.value)}
              disabled={assignBusy}
            >
              {tenants.map((t) => (
                <option key={t.id} value={t.id}>
                  {t.name} ({t.slug})
                </option>
              ))}
            </select>
          </label>
          <label className="billing-page__field">
            Plano SaaS
            <select
              className="auth-field__input"
              value={selectedPlanId}
              onChange={(e) => setSelectedPlanId(e.target.value)}
              disabled={assignBusy}
            >
              {plans.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name}
                </option>
              ))}
            </select>
          </label>
          <label className="billing-page__field">
            Início
            <input
              className="auth-field__input"
              type="date"
              value={assignStart}
              onChange={(e) => setAssignStart(e.target.value)}
              disabled={assignBusy}
            />
          </label>
          <label className="billing-page__field">
            Fim (opcional)
            <input
              className="auth-field__input"
              type="date"
              value={assignEnd}
              onChange={(e) => setAssignEnd(e.target.value)}
              disabled={assignBusy}
            />
          </label>
          <label className="billing-page__field">
            Ciclo
            <select
              className="auth-field__input"
              value={assignCycle}
              onChange={(e) => setAssignCycle(Number(e.target.value) as BillingCycle)}
              disabled={assignBusy}
            >
              <option value={BillingCycle.Monthly}>Mensal</option>
              <option value={BillingCycle.Yearly}>Anual</option>
            </select>
          </label>
          <div className="billing-page__actions bo-field-full">
            <button type="submit" disabled={assignBusy}>
              Atribuir
            </button>
          </div>
        </form>
      </div>

      <div className="bo-panel bo-divider-top">
        <h2 className="bo-subtitle">Tenants por plano</h2>
        <label className="billing-page__field">
          Plano
          <select
            className="auth-field__input"
            value={selectedPlanId}
            onChange={(e) => {
              setSelectedPlanId(e.target.value)
              setByPlanPage(1)
            }}
          >
            {plans.map((p) => (
              <option key={p.id} value={p.id}>
                {p.name}
              </option>
            ))}
          </select>
        </label>
        {byPlanError ? (
          <p className="billing-page__error" role="alert">
            {byPlanError}
          </p>
        ) : null}
        {byPlanLoading ? <p className="bo-muted">Carregando…</p> : null}
        {!byPlanLoading && byPlanItems.length === 0 ? <p className="bo-muted">Nenhum tenant nesta página.</p> : null}
        {!byPlanLoading && byPlanItems.length > 0 ? (
          <div className="bo-table-wrap">
            <table className="bo-table">
              <thead>
                <tr>
                  <th>Tenant</th>
                  <th>Início</th>
                  <th>Status</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {byPlanItems.map((row) => (
                  <tr key={`${row.tenantId}-${row.startDate}`}>
                    <td>{row.tenantName}</td>
                    <td>{new Date(row.startDate).toLocaleDateString()}</td>
                    <td>{formatTenantPlanStatus(row.status)}</td>
                    <td>
                      <Link to={`/backoffice/tenants/${row.tenantId}`}>Abrir</Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : null}
        {byPlanTotal > 0 ? (
          <div className="bo-pager">
            <button
              type="button"
              disabled={byPlanPage <= 1 || byPlanLoading}
              onClick={() => void loadByPlan(selectedPlanId, byPlanPage - 1)}
            >
              Anterior
            </button>
            <span>
              Página {byPlanPage} de {totalPages} ({byPlanTotal} vínculos)
            </span>
            <button
              type="button"
              disabled={byPlanLoading || byPlanPage >= totalPages}
              onClick={() => void loadByPlan(selectedPlanId, byPlanPage + 1)}
            >
              Próxima
            </button>
          </div>
        ) : null}
      </div>
    </section>
  )
}
