import { Elements } from '@stripe/react-stripe-js'
import { loadStripe, type StripeElementsOptions } from '@stripe/stripe-js'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { getApiErrorMessage } from '../../shared/auth'
import {
  formatBillingCycle,
  formatBillingInvoiceStatus,
  formatBillingSubscriptionStatus,
} from '../../shared/backoffice/formatters'
import type { TenantSaasPaymentMethodDto } from '../../shared/backoffice/types'
import {
  createAdminSaasSetupIntent,
  deleteAdminSaasCard,
  getAdminSaasStripeConfig,
  getAdminSaasSubscription,
  listAdminSaasCards,
  listAdminSaasInvoices,
} from '../../shared/payments/adminSaasPaymentsApi'
import { AdminSaasAddCardForm } from './AdminSaasAddCardForm'

export function AdminBillingPage() {
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [publishableKey, setPublishableKey] = useState<string | null>(null)
  const [subscription, setSubscription] = useState<Awaited<
    ReturnType<typeof getAdminSaasSubscription>
  > | null>(null)
  const [invoices, setInvoices] = useState<Awaited<ReturnType<typeof listAdminSaasInvoices>> | null>(
    null,
  )
  const [cards, setCards] = useState<TenantSaasPaymentMethodDto[]>([])
  const [cardsError, setCardsError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [setupClientSecret, setSetupClientSecret] = useState<string | null>(null)
  const [showAddCard, setShowAddCard] = useState(false)

  const stripePromise = useMemo(
    () => (publishableKey ? loadStripe(publishableKey) : null),
    [publishableKey],
  )

  const reload = useCallback(async () => {
    setError(null)
    setCardsError(null)
    try {
      const [cfg, sub] = await Promise.all([getAdminSaasStripeConfig(), getAdminSaasSubscription()])
      setPublishableKey(cfg.publishableKey?.trim() || null)
      setSubscription(sub)
      if (!sub) {
        setInvoices(null)
        setCards([])
        return
      }
      const [inv, list] = await Promise.all([
        listAdminSaasInvoices(1, 20),
        listAdminSaasCards().catch((e: unknown) => {
          setCardsError(getApiErrorMessage(e, 'Erro ao listar cartões.'))
          return [] as TenantSaasPaymentMethodDto[]
        }),
      ])
      setInvoices(inv)
      setCards(list)
    } catch (e: unknown) {
      setError(getApiErrorMessage(e, 'Erro ao carregar faturamento SaaS.'))
    }
  }, [])

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    void reload().finally(() => {
      if (!cancelled) setLoading(false)
    })
    return () => {
      cancelled = true
    }
  }, [reload])

  async function onStartAddCard() {
    if (!publishableKey) return
    setBusy(true)
    setCardsError(null)
    try {
      const si = await createAdminSaasSetupIntent()
      setSetupClientSecret(si.clientSecret)
      setShowAddCard(true)
    } catch (e: unknown) {
      setCardsError(getApiErrorMessage(e, 'Erro ao preparar cadastro de cartão.'))
    } finally {
      setBusy(false)
    }
  }

  function onCancelAddCard() {
    setShowAddCard(false)
    setSetupClientSecret(null)
  }

  async function onDeleteCard(id: string) {
    if (!window.confirm('Remover este cartão?')) return
    setBusy(true)
    setCardsError(null)
    try {
      await deleteAdminSaasCard(id)
      await reload()
    } catch (e: unknown) {
      setCardsError(getApiErrorMessage(e, 'Não foi possível remover o cartão.'))
    } finally {
      setBusy(false)
    }
  }

  const elementsOptions: StripeElementsOptions | undefined =
    setupClientSecret ? { clientSecret: setupClientSecret } : undefined

  if (loading) {
    return (
      <section>
        <h1>Faturamento SaaS</h1>
        <p className="bo-muted">Carregando…</p>
      </section>
    )
  }

  return (
    <section className="admin-billing-page">
      <h1>Faturamento SaaS</h1>
      <p className="bo-muted">
        Cobrança da <strong>plataforma</strong> pelo uso do software (plano comercial). É independente do
        gateway de sócios (Stripe direto ou outro provedor configurado pelo clube).
      </p>

      {error ? (
        <p className="billing-page__error" role="alert">
          {error}
        </p>
      ) : null}

      {subscription == null ? (
        <p role="status">
          Não há assinatura ativa de faturamento SaaS. O{' '}
          <strong>operador da plataforma</strong> inicia a cobrança no backoffice após vincular um plano ao
          clube.
        </p>
      ) : (
        <>
          <h2>Assinatura</h2>
          <ul className="admin-billing-page__meta">
            <li>
              Valor: <strong>{subscription.recurringAmount.toFixed(2)}</strong> {subscription.currency} /{' '}
              {formatBillingCycle(subscription.billingCycle)}
            </li>
            <li>Status: {formatBillingSubscriptionStatus(subscription.status)}</li>
            {subscription.nextBillingAtUtc ? (
              <li>Próxima cobrança: {new Date(subscription.nextBillingAtUtc).toLocaleString()}</li>
            ) : null}
          </ul>
        </>
      )}

      {invoices ? (
        <>
          <h2>Faturas recentes</h2>
          {invoices.items.length === 0 ? (
            <p className="bo-muted">Nenhuma fatura.</p>
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
                {invoices.items.map((inv) => (
                  <tr key={inv.id}>
                    <td>{new Date(inv.dueAtUtc).toLocaleDateString()}</td>
                    <td>
                      {inv.amount.toFixed(2)} {inv.currency}
                    </td>
                    <td>{formatBillingInvoiceStatus(inv.status)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </>
      ) : null}

      <h2>Cartões de crédito</h2>
      <p className="bo-muted">
        Os cartões ficam salvos na Stripe (conta da plataforma) para cobrança da assinatura SaaS. Você pode
        cadastrar, excluir e definir o cartão padrão ao salvar um novo cartão.
      </p>

      {cardsError ? (
        <p className="billing-page__error" role="alert">
          {cardsError}
        </p>
      ) : null}

      {subscription == null ? null : !publishableKey ? (
        <p role="status">
          Para cadastrar cartões nesta tela, a API precisa expor a chave publicável Stripe (
          <code>Payments:StripePublishableKey</code>). Sem ela, use o portal do cliente Stripe pelo
          operador da plataforma no backoffice.
        </p>
      ) : null}

      {subscription != null && publishableKey && cards.length === 0 && !showAddCard ? (
        <p className="bo-muted">Nenhum cartão cadastrado.</p>
      ) : null}

      {subscription != null && cards.length > 0 ? (
        <ul className="admin-billing-page__cards">
          {cards.map((c) => (
            <li key={c.id}>
              <span className="admin-billing-page__card-brand">{c.brand}</span> •••• {c.last4}{' '}
              {c.expMonth}/{c.expYear}
              {c.isDefault ? <span className="admin-billing-page__badge">Padrão</span> : null}
              <button
                type="button"
                className="btn-danger"
                disabled={busy}
                onClick={() => void onDeleteCard(c.id)}
              >
                Remover
              </button>
            </li>
          ))}
        </ul>
      ) : null}

      {subscription != null && publishableKey && !showAddCard ? (
        <p>
          <button type="button" disabled={busy} onClick={() => void onStartAddCard()}>
            Adicionar cartão
          </button>
        </p>
      ) : null}

      {subscription != null && publishableKey && showAddCard && stripePromise && elementsOptions ? (
        <Elements stripe={stripePromise} options={elementsOptions}>
          <AdminSaasAddCardForm
            onCancel={onCancelAddCard}
            onSuccess={() => {
              onCancelAddCard()
              void reload()
            }}
          />
        </Elements>
      ) : null}

      <h2>Sócios</h2>
      <p className="bo-muted">
        Mensalidades e PIX de sócios usam o <strong>gateway configurado pelo clube</strong> (Stripe direto,
        quando ativado no backoffice) — veja <a href="/admin/stripe">Gateway de pagamentos</a> e a área do
        sócio em Pagamentos.
      </p>
    </section>
  )
}
