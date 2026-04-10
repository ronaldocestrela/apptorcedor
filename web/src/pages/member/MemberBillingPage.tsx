import { useCallback, useEffect, useState } from 'react'
import {
  createMemberPixCheckout,
  fetchMemberPlans,
  getMySubscription,
  listMyInvoices,
  subscribeMemberPlan,
  type MemberBillingInvoice,
  type MemberBillingSubscription,
  type MemberPlanSummary,
} from '../../shared/payments/paymentsApi'

/** Alinhado a `BillingSubscriptionStatus` no backend (valores numéricos do enum). */
const BILLING_SUBSCRIPTION_STATUS_LABELS: Record<number, string> = {
  0: 'Pendente',
  1: 'Ativa',
  2: 'Inadimplente',
  3: 'Cancelada',
}

function formatBillingSubscriptionStatus(status: number): string {
  return BILLING_SUBSCRIPTION_STATUS_LABELS[status] ?? `Desconhecido (${status})`
}

/** Exibe só a data (dd/mm/aaaa) em fuso UTC, para valores vindos como `nextBillingAtUtc` sem sufixo Z. */
function formatBillingDateOnlyBr(iso: string | null | undefined): string {
  if (iso == null || iso === '') return '—'
  const hasTz = /[zZ]$|[+-]\d{2}:?\d{2}$/.test(iso.trim())
  const normalized = hasTz ? iso.trim() : `${iso.trim()}Z`
  const t = Date.parse(normalized)
  if (Number.isNaN(t)) return iso
  return new Date(t).toLocaleDateString('pt-BR', { timeZone: 'UTC' })
}

export function MemberBillingPage() {
  const [plans, setPlans] = useState<MemberPlanSummary[]>([])
  const [selectedPlanId, setSelectedPlanId] = useState('')
  const [subscription, setSubscription] = useState<MemberBillingSubscription | null | undefined>(undefined)
  const [invoices, setInvoices] = useState<MemberBillingInvoice[]>([])
  const [pixPayload, setPixPayload] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    setError(null)
    try {
      const [plansRes, sub, inv] = await Promise.all([
        fetchMemberPlans(1, 50),
        getMySubscription(),
        listMyInvoices(1, 20),
      ])
      const active = plansRes.items.filter((p) => p.isActive)
      setPlans(active)
      setSubscription(sub)
      setInvoices(inv.items)
      setSelectedPlanId((prev) => {
        if (prev) return prev
        const first = active[0]
        return first ? first.id : ''
      })
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar dados de pagamento.')
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  async function onSubscribe() {
    if (!selectedPlanId) return
    setBusy(true)
    setError(null)
    try {
      await subscribeMemberPlan(selectedPlanId, 1)
      await load()
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Falha na assinatura.')
    } finally {
      setBusy(false)
    }
  }

  async function onPix() {
    if (!selectedPlanId) return
    setBusy(true)
    setError(null)
    setPixPayload(null)
    try {
      const r = await createMemberPixCheckout(selectedPlanId)
      setPixPayload(r.pixCopyPaste ?? null)
      await load()
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Falha ao gerar PIX.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="billing-page">
      <h1>Pagamentos — sócio</h1>
      <p className="billing-page__hint">
        Escolha um plano ativo do clube, assine e gere cobrança PIX (provider stub no backend).
      </p>

      {error ? <p className="billing-page__error">{error}</p> : null}

      <div className="billing-page__block">
        <h2>Planos</h2>
        {plans.length === 0 ? (
          <p>Nenhum plano ativo. Um administrador precisa cadastrar planos em <code>/api/plans</code>.</p>
        ) : (
          <label className="billing-page__field">
            Plano
            <select
              value={selectedPlanId}
              onChange={(e) => setSelectedPlanId(e.target.value)}
              disabled={busy}
            >
              {plans.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.nome} — {p.preco.toFixed(2)} BRL
                </option>
              ))}
            </select>
          </label>
        )}
        <div className="billing-page__actions">
          <button type="button" disabled={busy || !selectedPlanId} onClick={() => void onSubscribe()}>
            Assinar plano
          </button>
          <button type="button" disabled={busy || !selectedPlanId} onClick={() => void onPix()}>
            Gerar PIX
          </button>
          <button type="button" disabled={busy} onClick={() => void load()}>
            Atualizar
          </button>
        </div>
      </div>

      <div className="billing-page__block">
        <h2>Assinatura atual</h2>
        {subscription === undefined ? (
          <p>Carregando…</p>
        ) : subscription === null ? (
          <p>Sem assinatura ativa.</p>
        ) : (
          <ul>
            <li>
              Plano: {subscription.planName ?? '—'} <span className="billing-page__hint">({subscription.memberPlanId})</span>
            </li>
            <li>Valor: {subscription.recurringAmount} {subscription.currency}</li>
            <li>Status: {formatBillingSubscriptionStatus(subscription.status)}</li>
            <li>Próxima cobrança: {formatBillingDateOnlyBr(subscription.nextBillingAtUtc)}</li>
          </ul>
        )}
      </div>

      {pixPayload ? (
        <div className="billing-page__block">
          <h2>PIX (copia e cola)</h2>
          <textarea readOnly rows={4} className="billing-page__pix" value={pixPayload} />
        </div>
      ) : null}

      <div className="billing-page__block">
        <h2>Faturas recentes</h2>
        {invoices.length === 0 ? (
          <p>Nenhuma fatura.</p>
        ) : (
          <table className="billing-page__table">
            <thead>
              <tr>
                <th>Vencimento</th>
                <th>Valor</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {invoices.map((i) => (
                <tr key={i.id}>
                  <td>{new Date(i.dueAtUtc).toLocaleString()}</td>
                  <td>
                    {i.amount} {i.currency}
                  </td>
                  <td>{i.status}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </section>
  )
}
