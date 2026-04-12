import { useCallback, useEffect, useState } from 'react'
import {
  createMemberPixCheckout,
  createMemberStripeCheckoutSession,
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

/** Domínio: `PaymentMethodKind` — Pix=1, Card=2 */
const PAYMENT_METHOD_PIX = 1 as const

type PaymentChoice = 'pix' | 'card_credit' | 'card_debit'

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

function readAxiosMessage(e: unknown): string {
  if (e != null && typeof e === 'object' && 'response' in e) {
    const data = (e as { response?: { data?: { message?: string } } }).response?.data
    if (data?.message && typeof data.message === 'string') return data.message
  }
  return e instanceof Error ? e.message : 'Erro inesperado.'
}

export function MemberBillingPage() {
  const [plans, setPlans] = useState<MemberPlanSummary[]>([])
  const [selectedPlanId, setSelectedPlanId] = useState('')
  const [paymentChoice, setPaymentChoice] = useState<PaymentChoice>('card_credit')
  const [subscription, setSubscription] = useState<MemberBillingSubscription | null | undefined>(undefined)
  const [invoices, setInvoices] = useState<MemberBillingInvoice[]>([])
  const [pixPayload, setPixPayload] = useState<string | null>(null)
  const [checkoutFlash, setCheckoutFlash] = useState<'success' | 'cancel' | null>(null)
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
      setError(readAxiosMessage(e) || 'Erro ao carregar dados de pagamento.')
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  /** Volta do Stripe Checkout: feedback e remove query string. */
  useEffect(() => {
    const params = new URLSearchParams(window.location.search)
    const v = params.get('checkout')
    if (v === 'success' || v === 'cancel') {
      setCheckoutFlash(v)
      params.delete('checkout')
      const qs = params.toString()
      const newUrl = `${window.location.pathname}${qs ? `?${qs}` : ''}${window.location.hash}`
      window.history.replaceState({}, '', newUrl)
    }
  }, [])

  async function onPayWithPix() {
    if (!selectedPlanId) return
    setBusy(true)
    setError(null)
    setPixPayload(null)
    try {
      await subscribeMemberPlan(selectedPlanId, PAYMENT_METHOD_PIX)
      const r = await createMemberPixCheckout(selectedPlanId)
      setPixPayload(r.pixCopyPaste ?? null)
      await load()
    } catch (e: unknown) {
      setError(readAxiosMessage(e) || 'Falha ao assinar ou gerar PIX.')
    } finally {
      setBusy(false)
    }
  }

  async function onPayWithCard() {
    if (!selectedPlanId) return
    setBusy(true)
    setError(null)
    try {
      const base = `${window.location.origin}${window.location.pathname}`
      const successUrl = `${base}?checkout=success`
      const cancelUrl = `${base}?checkout=cancel`
      const session = await createMemberStripeCheckoutSession(selectedPlanId, successUrl, cancelUrl)
      window.location.assign(session.url)
    } catch (e: unknown) {
      setError(readAxiosMessage(e) || 'Falha ao abrir o pagamento com cartão.')
      setBusy(false)
    }
  }

  async function onPrimaryPay() {
    if (paymentChoice === 'pix') {
      await onPayWithPix()
      return
    }
    await onPayWithCard()
  }

  const primaryLabel =
    paymentChoice === 'pix'
      ? 'Assinar e gerar PIX'
      : paymentChoice === 'card_credit'
        ? 'Pagar com cartão de crédito (Stripe Checkout)'
        : 'Pagar com cartão de débito (Stripe Checkout)'

  return (
    <section className="billing-page">
      <h1>Pagamentos — sócio</h1>
      <p className="billing-page__hint">
        Escolha um plano ativo, o meio de pagamento e conclua a assinatura. Pagamento com cartão ocorre na página segura da
        Stripe (Checkout).
      </p>

      {checkoutFlash === 'success' ? (
        <p className="billing-page__hint" role="status">
          Retorno do checkout: você pode atualizar os dados abaixo para ver a assinatura após confirmação.
        </p>
      ) : null}
      {checkoutFlash === 'cancel' ? (
        <p className="billing-page__hint" role="status">
          Pagamento com cartão cancelado na Stripe. Você pode tentar novamente quando quiser.
        </p>
      ) : null}

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
        <fieldset className="billing-page__field" style={{ border: 'none', padding: 0, margin: '0.75rem 0 0' }}>
          <legend className="billing-page__hint" style={{ marginBottom: '0.35rem' }}>
            Forma de pagamento
          </legend>
          <label className="billing-page__field" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <input
              type="radio"
              name="pay"
              checked={paymentChoice === 'pix'}
              onChange={() => setPaymentChoice('pix')}
              disabled={busy}
            />
            PIX
          </label>
          <label className="billing-page__field" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <input
              type="radio"
              name="pay"
              checked={paymentChoice === 'card_credit'}
              onChange={() => setPaymentChoice('card_credit')}
              disabled={busy}
            />
            Cartão de crédito
          </label>
          <label className="billing-page__field" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <input
              type="radio"
              name="pay"
              checked={paymentChoice === 'card_debit'}
              onChange={() => setPaymentChoice('card_debit')}
              disabled={busy}
            />
            Cartão de débito
          </label>
          <p className="billing-page__hint" style={{ marginTop: '0.5rem' }}>
            Crédito e débito usam o mesmo fluxo de cartão na Stripe; a bandeira/emissor define o tipo na hora do pagamento.
          </p>
        </fieldset>
        <div className="billing-page__actions">
          <button type="button" disabled={busy || !selectedPlanId} onClick={() => void onPrimaryPay()}>
            {primaryLabel}
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
