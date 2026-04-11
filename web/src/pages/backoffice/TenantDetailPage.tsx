import axios from 'axios'
import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { getApiErrorMessage } from '../../shared/auth'
import {
  addTenantDomain,
  addTenantSetting,
  assignPlanToTenant,
  changeTenantStatus,
  createTenantSaasPortalSession,
  getStripeConnectStatus,
  getTenantById,
  getTenantPlanByTenant,
  getTenantSaasSubscription,
  listSaaSPlans,
  listTenantSaasInvoices,
  removeTenantDomain,
  removeTenantSetting,
  revokeTenantPlan,
  startStripeConnectOnboarding,
  startTenantSaasBilling,
  updateTenant,
  updateTenantSetting,
  type SaaSPlanDto,
  type TenantDetailDto,
  type TenantPlanDto,
  type TenantSaasBillingInvoiceDto,
  type TenantSaasBillingSubscriptionDto,
} from '../../shared/backoffice'
import { BillingCycle, TenantStatus } from '../../shared/backoffice/types'
import {
  formatBillingCycle,
  formatBillingInvoiceStatus,
  formatBillingSubscriptionStatus,
  formatTenantPlanStatus,
  formatTenantStatus,
} from '../../shared/backoffice/formatters'

const TABS = ['geral', 'dominios', 'config', 'plano', 'pagamentos', 'stripe'] as const
type TabId = (typeof TABS)[number]

function isTabId(s: string | null): s is TabId {
  return s != null && (TABS as readonly string[]).includes(s)
}

export function TenantDetailPage() {
  const { id: tenantId = '' } = useParams<{ id: string }>()
  const [searchParams, setSearchParams] = useSearchParams()
  const tabParam = searchParams.get('tab')
  const activeTab: TabId = isTabId(tabParam) ? tabParam : 'geral'

  const setTab = useCallback(
    (t: TabId) => {
      setSearchParams({ tab: t })
    },
    [setSearchParams],
  )

  const [tenant, setTenant] = useState<TenantDetailDto | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  const [name, setName] = useState('')
  const [connectionString, setConnectionString] = useState('')
  const [statusDraft, setStatusDraft] = useState<TenantStatus>(TenantStatus.Active)
  const [saveBusy, setSaveBusy] = useState(false)
  const [saveMsg, setSaveMsg] = useState<string | null>(null)

  const [newOrigin, setNewOrigin] = useState('')
  const [domainBusy, setDomainBusy] = useState(false)

  const [newKey, setNewKey] = useState('')
  const [newValue, setNewValue] = useState('')
  const [settingBusy, setSettingBusy] = useState(false)
  const [editingSettingId, setEditingSettingId] = useState<string | null>(null)
  const [editingSettingValue, setEditingSettingValue] = useState('')

  const [tenantPlan, setTenantPlan] = useState<TenantPlanDto | null>(null)
  const [planLoaded, setPlanLoaded] = useState(false)
  const [planLoadError, setPlanLoadError] = useState<string | null>(null)
  const [saasPlans, setSaasPlans] = useState<SaaSPlanDto[]>([])
  const [assignPlanId, setAssignPlanId] = useState('')
  const [assignStart, setAssignStart] = useState(() => new Date().toISOString().slice(0, 10))
  const [assignEnd, setAssignEnd] = useState('')
  const [assignCycle, setAssignCycle] = useState<BillingCycle>(BillingCycle.Monthly)
  const [planBusy, setPlanBusy] = useState(false)

  const [subscription, setSubscription] = useState<TenantSaasBillingSubscriptionDto | null>(null)
  const [invoices, setInvoices] = useState<TenantSaasBillingInvoiceDto[]>([])
  const [invPage, setInvPage] = useState(1)
  const [invTotal, setInvTotal] = useState(0)
  const [payBusy, setPayBusy] = useState(false)
  const [payError, setPayError] = useState<string | null>(null)

  const [connectStatus, setConnectStatus] = useState<Awaited<ReturnType<typeof getStripeConnectStatus>> | null>(null)
  const [connectBusy, setConnectBusy] = useState(false)

  const reloadTenant = useCallback(async () => {
    if (!tenantId) return
    setLoadError(null)
    setLoading(true)
    try {
      const t = await getTenantById(tenantId)
      setTenant(t)
      setName(t.name)
      setConnectionString(t.connectionString ?? '')
      setStatusDraft(t.status)
    } catch (e: unknown) {
      setLoadError(getApiErrorMessage(e, 'Tenant não encontrado.'))
      setTenant(null)
    } finally {
      setLoading(false)
    }
  }, [tenantId])

  useEffect(() => {
    void reloadTenant()
  }, [reloadTenant])

  const reloadPlan = useCallback(async () => {
    if (!tenantId) return
    setPlanLoadError(null)
    setPlanLoaded(false)
    try {
      const p = await getTenantPlanByTenant(tenantId)
      setTenantPlan(p)
    } catch (e: unknown) {
      if (axios.isAxiosError(e) && e.response?.status === 404) {
        setTenantPlan(null)
      } else {
        setPlanLoadError(getApiErrorMessage(e, 'Erro ao carregar plano do tenant.'))
        setTenantPlan(null)
      }
    } finally {
      setPlanLoaded(true)
    }
  }, [tenantId])

  useEffect(() => {
    if (activeTab !== 'plano') return
    void reloadPlan()
    void listSaaSPlans(1, 100).then((r) => {
      const active = r.items.filter((x) => x.isActive)
      setSaasPlans(active.length ? active : r.items)
      setAssignPlanId((prev) => {
        if (prev) return prev
        const list = active.length ? active : r.items
        const first = list[0]?.id
        return first ?? ''
      })
    })
  }, [activeTab, reloadPlan])

  const reloadBilling = useCallback(async () => {
    if (!tenantId) return
    setPayError(null)
    try {
      const sub = await getTenantSaasSubscription(tenantId)
      setSubscription(sub)
      const inv = await listTenantSaasInvoices(tenantId, invPage, 10)
      setInvoices(inv.items)
      setInvTotal(inv.totalCount)
    } catch (e: unknown) {
      setPayError(getApiErrorMessage(e, 'Erro ao carregar cobrança SaaS.'))
    }
  }, [tenantId, invPage])

  useEffect(() => {
    if (activeTab === 'pagamentos') {
      void reloadBilling()
    }
  }, [activeTab, reloadBilling])

  const reloadConnect = useCallback(async () => {
    if (!tenantId) return
    try {
      const s = await getStripeConnectStatus(tenantId)
      setConnectStatus(s)
    } catch {
      setConnectStatus(null)
    }
  }, [tenantId])

  useEffect(() => {
    if (activeTab === 'stripe') {
      void reloadConnect()
    }
  }, [activeTab, reloadConnect])

  async function onSaveGeneral(e: FormEvent) {
    e.preventDefault()
    if (!tenantId) return
    setSaveMsg(null)
    setSaveBusy(true)
    try {
      await updateTenant(tenantId, {
        name: name.trim() || null,
        connectionString: connectionString.trim() || null,
      })
      await reloadTenant()
      setSaveMsg('Dados salvos.')
    } catch (err: unknown) {
      setSaveMsg(getApiErrorMessage(err, 'Falha ao salvar.'))
    } finally {
      setSaveBusy(false)
    }
  }

  async function onApplyStatus(e: FormEvent) {
    e.preventDefault()
    if (!tenantId) return
    setSaveMsg(null)
    setSaveBusy(true)
    try {
      await changeTenantStatus(tenantId, { status: statusDraft })
      await reloadTenant()
      setSaveMsg('Status atualizado.')
    } catch (err: unknown) {
      setSaveMsg(getApiErrorMessage(err, 'Falha ao alterar status.'))
    } finally {
      setSaveBusy(false)
    }
  }

  async function onAddDomain(e: FormEvent) {
    e.preventDefault()
    if (!tenantId || !newOrigin.trim()) return
    setDomainBusy(true)
    try {
      await addTenantDomain(tenantId, { origin: newOrigin.trim() })
      setNewOrigin('')
      await reloadTenant()
    } catch (err: unknown) {
      setSaveMsg(getApiErrorMessage(err, 'Falha ao adicionar domínio.'))
    } finally {
      setDomainBusy(false)
    }
  }

  async function onRemoveDomain(domainId: string) {
    if (!tenantId) return
    setDomainBusy(true)
    try {
      await removeTenantDomain(tenantId, domainId)
      await reloadTenant()
    } catch (err: unknown) {
      setSaveMsg(getApiErrorMessage(err, 'Falha ao remover domínio.'))
    } finally {
      setDomainBusy(false)
    }
  }

  async function onAddSetting(e: FormEvent) {
    e.preventDefault()
    if (!tenantId || !newKey.trim()) return
    setSettingBusy(true)
    try {
      await addTenantSetting(tenantId, { key: newKey.trim(), value: newValue })
      setNewKey('')
      setNewValue('')
      await reloadTenant()
    } catch (err: unknown) {
      setSaveMsg(getApiErrorMessage(err, 'Falha ao adicionar configuração.'))
    } finally {
      setSettingBusy(false)
    }
  }

  async function onSaveSetting(settingId: string) {
    if (!tenantId) return
    setSettingBusy(true)
    try {
      await updateTenantSetting(tenantId, settingId, { value: editingSettingValue })
      setEditingSettingId(null)
      await reloadTenant()
    } catch (err: unknown) {
      setSaveMsg(getApiErrorMessage(err, 'Falha ao atualizar configuração.'))
    } finally {
      setSettingBusy(false)
    }
  }

  async function onRemoveSetting(settingId: string) {
    if (!tenantId) return
    setSettingBusy(true)
    try {
      await removeTenantSetting(tenantId, settingId)
      await reloadTenant()
    } catch (err: unknown) {
      setSaveMsg(getApiErrorMessage(err, 'Falha ao remover configuração.'))
    } finally {
      setSettingBusy(false)
    }
  }

  async function onAssignPlan(e: FormEvent) {
    e.preventDefault()
    if (!tenantId || !assignPlanId) return
    setPlanBusy(true)
    setPlanLoadError(null)
    try {
      const start = new Date(assignStart + 'T12:00:00.000Z').toISOString()
      const end =
        assignEnd.trim() === '' ? null : new Date(assignEnd + 'T12:00:00.000Z').toISOString()
      await assignPlanToTenant({
        tenantId,
        saaSPlanId: assignPlanId,
        startDate: start,
        endDate: end,
        billingCycle: assignCycle,
      })
      await reloadPlan()
    } catch (err: unknown) {
      setPlanLoadError(getApiErrorMessage(err, 'Falha ao atribuir plano.'))
    } finally {
      setPlanBusy(false)
    }
  }

  async function onRevokePlan() {
    if (!tenantPlan) return
    setPlanBusy(true)
    setPlanLoadError(null)
    try {
      await revokeTenantPlan(tenantPlan.id)
      await reloadPlan()
    } catch (err: unknown) {
      setPlanLoadError(getApiErrorMessage(err, 'Falha ao revogar vínculo.'))
    } finally {
      setPlanBusy(false)
    }
  }

  async function onStartBilling() {
    if (!tenantId) return
    setPayBusy(true)
    setPayError(null)
    try {
      await startTenantSaasBilling(tenantId)
      await reloadBilling()
    } catch (err: unknown) {
      setPayError(getApiErrorMessage(err, 'Falha ao iniciar cobrança.'))
    } finally {
      setPayBusy(false)
    }
  }

  async function onPortal() {
    if (!tenantId) return
    setPayBusy(true)
    setPayError(null)
    try {
      const returnUrl = `${window.location.origin}/backoffice/tenants/${tenantId}?tab=pagamentos`
      const { url } = await createTenantSaasPortalSession(tenantId, returnUrl)
      window.location.assign(url)
    } catch (err: unknown) {
      setPayError(getApiErrorMessage(err, 'Falha ao abrir portal.'))
    } finally {
      setPayBusy(false)
    }
  }

  async function onStripeOnboarding() {
    if (!tenantId) return
    setConnectBusy(true)
    try {
      const base = `${window.location.origin}/backoffice/tenants/${tenantId}?tab=stripe`
      const { url } = await startStripeConnectOnboarding(tenantId, {
        refreshUrl: base,
        returnUrl: base,
      })
      window.open(url, '_blank', 'noopener,noreferrer')
      await reloadConnect()
    } catch (err: unknown) {
      setSaveMsg(getApiErrorMessage(err, 'Falha no onboarding Connect.'))
    } finally {
      setConnectBusy(false)
    }
  }

  if (loading) {
    return (
      <section className="bo-page">
        <p className="bo-muted">Carregando…</p>
      </section>
    )
  }

  if (loadError || !tenant) {
    return (
      <section className="bo-page">
        <p className="billing-page__error" role="alert">
          {loadError ?? 'Tenant inválido.'}
        </p>
        <Link to="/backoffice/tenants">← Voltar</Link>
      </section>
    )
  }

  return (
    <section className="bo-page">
      <div className="bo-page__head">
        <div>
          <Link to="/backoffice/tenants" className="bo-back-link">
            ← Tenants
          </Link>
          <h1 className="bo-page__title">{tenant.name}</h1>
          <p className="bo-page__meta">
            <code>{tenant.slug}</code> · {formatTenantStatus(tenant.status)}
          </p>
        </div>
      </div>

      {saveMsg ? (
        <p className={saveMsg.includes('Falha') ? 'billing-page__error' : 'bo-hint'} role="status">
          {saveMsg}
        </p>
      ) : null}

      <div className="bo-tabs" role="tablist" aria-label="Seções do tenant">
        {(
          [
            ['geral', 'Geral'],
            ['dominios', 'Domínios'],
            ['config', 'Configurações'],
            ['plano', 'Plano SaaS'],
            ['pagamentos', 'Pagamentos SaaS'],
            ['stripe', 'Stripe Connect'],
          ] as const
        ).map(([id, label]) => (
          <button
            key={id}
            type="button"
            role="tab"
            aria-selected={activeTab === id}
            className={activeTab === id ? 'bo-tab bo-tab--active' : 'bo-tab'}
            onClick={() => setTab(id)}
          >
            {label}
          </button>
        ))}
      </div>

      {activeTab === 'geral' ? (
        <div className="bo-panel">
          <form className="bo-form-grid" onSubmit={onSaveGeneral}>
            <label className="billing-page__field">
              Nome
              <input className="auth-field__input" value={name} onChange={(e) => setName(e.target.value)} />
            </label>
            <label className="billing-page__field bo-field-full">
              Connection string
              <textarea
                className="admin-plans__textarea"
                rows={3}
                value={connectionString}
                onChange={(e) => setConnectionString(e.target.value)}
              />
            </label>
            <div className="billing-page__actions">
              <button type="submit" disabled={saveBusy}>
                Salvar dados
              </button>
            </div>
          </form>
          <form className="bo-form-grid bo-divider-top" onSubmit={onApplyStatus}>
            <label className="billing-page__field">
              Alterar status
              <select
                className="auth-field__input"
                value={statusDraft}
                onChange={(e) => setStatusDraft(Number(e.target.value) as TenantStatus)}
              >
                <option value={TenantStatus.Active}>Ativo</option>
                <option value={TenantStatus.Suspended}>Suspenso</option>
                <option value={TenantStatus.Inactive}>Inativo</option>
              </select>
            </label>
            <div className="billing-page__actions">
              <button type="submit" disabled={saveBusy}>
                Aplicar status
              </button>
            </div>
          </form>
        </div>
      ) : null}

      {activeTab === 'dominios' ? (
        <div className="bo-panel">
          <form className="bo-inline-form" onSubmit={onAddDomain}>
            <input
              type="url"
              placeholder="https://slug.exemplo.com"
              value={newOrigin}
              onChange={(e) => setNewOrigin(e.target.value)}
              disabled={domainBusy}
            />
            <button type="submit" disabled={domainBusy}>
              Adicionar origem
            </button>
          </form>
          <ul className="bo-list">
            {(tenant.domains ?? []).map((d) => (
              <li key={d.id} className="bo-list__row">
                <code>{d.origin}</code>
                <button type="button" disabled={domainBusy} onClick={() => void onRemoveDomain(d.id)}>
                  Remover
                </button>
              </li>
            ))}
          </ul>
          {(tenant.domains ?? []).length === 0 ? <p className="bo-muted">Nenhum domínio.</p> : null}
        </div>
      ) : null}

      {activeTab === 'config' ? (
        <div className="bo-panel">
          <form className="bo-inline-form" onSubmit={onAddSetting}>
            <input
              placeholder="Chave"
              value={newKey}
              onChange={(e) => setNewKey(e.target.value)}
              disabled={settingBusy}
            />
            <input
              placeholder="Valor"
              value={newValue}
              onChange={(e) => setNewValue(e.target.value)}
              disabled={settingBusy}
            />
            <button type="submit" disabled={settingBusy}>
              Adicionar
            </button>
          </form>
          <ul className="bo-list">
            {(tenant.settings ?? []).map((s) => (
              <li key={s.id} className="bo-list__row bo-list__row--stack">
                <strong>{s.key}</strong>
                {editingSettingId === s.id ? (
                  <div className="bo-inline-form">
                    <input
                      value={editingSettingValue}
                      onChange={(e) => setEditingSettingValue(e.target.value)}
                      disabled={settingBusy}
                    />
                    <button type="button" disabled={settingBusy} onClick={() => void onSaveSetting(s.id)}>
                      Salvar
                    </button>
                    <button
                      type="button"
                      disabled={settingBusy}
                      onClick={() => {
                        setEditingSettingId(null)
                      }}
                    >
                      Cancelar
                    </button>
                  </div>
                ) : (
                  <div className="bo-inline-form">
                    <code className="bo-setting-val">{s.value}</code>
                    <button
                      type="button"
                      disabled={settingBusy}
                      onClick={() => {
                        setEditingSettingId(s.id)
                        setEditingSettingValue(s.value)
                      }}
                    >
                      Editar
                    </button>
                    <button type="button" disabled={settingBusy} onClick={() => void onRemoveSetting(s.id)}>
                      Remover
                    </button>
                  </div>
                )}
              </li>
            ))}
          </ul>
        </div>
      ) : null}

      {activeTab === 'plano' ? (
        <div className="bo-panel">
          {planLoadError ? (
            <p className="billing-page__error" role="alert">
              {planLoadError}
            </p>
          ) : null}
          {!planLoaded ? <p className="bo-muted">Carregando plano…</p> : null}
          {planLoaded && tenantPlan ? (
            <div className="bo-stack">
              <p>
                <strong>Plano:</strong> {tenantPlan.planName}
              </p>
              <p>
                <strong>Ciclo:</strong> {formatBillingCycle(tenantPlan.billingCycle)}
              </p>
              <p>
                <strong>Status:</strong> {formatTenantPlanStatus(tenantPlan.status)}
              </p>
              <p>
                <strong>Início:</strong> {new Date(tenantPlan.startDate).toLocaleDateString()}
              </p>
              {tenantPlan.endDate ? (
                <p>
                  <strong>Fim:</strong> {new Date(tenantPlan.endDate).toLocaleDateString()}
                </p>
              ) : null}
              <button type="button" className="bo-btn-danger" disabled={planBusy} onClick={() => void onRevokePlan()}>
                Revogar vínculo
              </button>
            </div>
          ) : null}
          {planLoaded && !tenantPlan ? (
            <form className="bo-form-grid" onSubmit={onAssignPlan}>
              <p className="bo-field-full bo-muted">Nenhum plano ativo. Atribua um plano SaaS.</p>
              <label className="billing-page__field">
                Plano SaaS
                <select
                  className="auth-field__input"
                  value={assignPlanId}
                  onChange={(e) => setAssignPlanId(e.target.value)}
                >
                  {saasPlans.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.name} — {p.monthlyPrice} / mês
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
                />
              </label>
              <label className="billing-page__field">
                Fim (opcional)
                <input
                  className="auth-field__input"
                  type="date"
                  value={assignEnd}
                  onChange={(e) => setAssignEnd(e.target.value)}
                />
              </label>
              <label className="billing-page__field">
                Ciclo de cobrança
                <select
                  className="auth-field__input"
                  value={assignCycle}
                  onChange={(e) => setAssignCycle(Number(e.target.value) as BillingCycle)}
                >
                  <option value={BillingCycle.Monthly}>Mensal</option>
                  <option value={BillingCycle.Yearly}>Anual</option>
                </select>
              </label>
              <div className="billing-page__actions bo-field-full">
                <button type="submit" disabled={planBusy || !assignPlanId}>
                  Atribuir plano
                </button>
              </div>
            </form>
          ) : null}
        </div>
      ) : null}

      {activeTab === 'pagamentos' ? (
        <div className="bo-panel">
          {payError ? (
            <p className="billing-page__error" role="alert">
              {payError}
            </p>
          ) : null}
          <div className="bo-stack">
            <button type="button" disabled={payBusy} onClick={() => void onStartBilling()}>
              Iniciar cobrança SaaS (Stripe)
            </button>
            <button type="button" disabled={payBusy} onClick={() => void onPortal()}>
              Abrir portal do cliente (Stripe)
            </button>
          </div>
          {subscription ? (
            <div className="bo-stack bo-divider-top">
              <h3 className="bo-subtitle">Assinatura</h3>
              <p>Status: {formatBillingSubscriptionStatus(subscription.status)}</p>
              <p>
                Valor: {subscription.recurringAmount} {subscription.currency}
              </p>
              <p>Ciclo: {formatBillingCycle(subscription.billingCycle)}</p>
              {subscription.nextBillingAtUtc ? (
                <p>Próxima cobrança: {new Date(subscription.nextBillingAtUtc).toLocaleString()}</p>
              ) : null}
            </div>
          ) : (
            <p className="bo-muted">Sem assinatura SaaS ativa registrada.</p>
          )}
          <h3 className="bo-subtitle">Faturas</h3>
          {invoices.length === 0 ? <p className="bo-muted">Nenhuma fatura nesta página.</p> : null}
          {invoices.length > 0 ? (
            <div className="bo-table-wrap">
              <table className="bo-table">
                <thead>
                  <tr>
                    <th>Valor</th>
                    <th>Vencimento</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {invoices.map((inv) => (
                    <tr key={inv.id}>
                      <td>
                        {inv.amount} {inv.currency}
                      </td>
                      <td>{new Date(inv.dueAtUtc).toLocaleString()}</td>
                      <td>{formatBillingInvoiceStatus(inv.status)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : null}
          <div className="bo-pager">
            <button type="button" disabled={invPage <= 1 || payBusy} onClick={() => setInvPage((p) => p - 1)}>
              Anterior
            </button>
            <span>
              Página {invPage} ({invTotal} faturas)
            </span>
            <button
              type="button"
              disabled={payBusy || invPage * 10 >= invTotal || invTotal === 0}
              onClick={() => setInvPage((p) => p + 1)}
            >
              Próxima
            </button>
          </div>
        </div>
      ) : null}

      {activeTab === 'stripe' ? (
        <div className="bo-panel">
          <p className="bo-muted">
            Onboarding Express para o tenant receber pagamentos de sócios. Requer gateway Stripe configurado.
          </p>
          <button type="button" disabled={connectBusy} onClick={() => void onStripeOnboarding()}>
            Gerar link de onboarding
          </button>
          <button type="button" className="admin-plans__btn-secondary" disabled={connectBusy} onClick={() => void reloadConnect()}>
            Atualizar status
          </button>
          {connectStatus ? (
            <ul className="bo-list bo-divider-top">
              <li>Configurado: {connectStatus.isConfigured ? 'sim' : 'não'}</li>
              <li>Conta: {connectStatus.stripeAccountId ?? '—'}</li>
              <li>Onboarding (código): {connectStatus.onboardingStatus}</li>
              <li>Cobranças habilitadas: {connectStatus.chargesEnabled ? 'sim' : 'não'}</li>
              <li>Repasses habilitados: {connectStatus.payoutsEnabled ? 'sim' : 'não'}</li>
              <li>Detalhes enviados: {connectStatus.detailsSubmitted ? 'sim' : 'não'}</li>
            </ul>
          ) : (
            <p className="bo-muted">Status não disponível.</p>
          )}
        </div>
      ) : null}
    </section>
  )
}
