import { useEffect, useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { isAxiosError } from 'axios'
import {
  subscriptionsService,
  type MySubscriptionSummary,
  type TorcedorSubscriptionCheckoutResponse,
} from '../features/plans/subscriptionsService'

export type SubscriptionConfirmationLocationState = {
  checkout: TorcedorSubscriptionCheckoutResponse
  planName: string
  billingCycle: string
}

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

function formatPrice(value: number, currency: string): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: currency === 'BRL' ? 'BRL' : 'BRL' })
}

function formatDate(iso: string | null | undefined): string {
  if (!iso)
    return '—'
  const d = new Date(iso)
  if (Number.isNaN(d.getTime()))
    return iso
  return d.toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
}

function digitalCardStateLabel(state: string): string {
  switch (state) {
    case 'NotAssociated':
      return 'Sem associação ativa para carteirinha'
    case 'MembershipInactive':
      return 'Associação inativa'
    case 'AwaitingIssuance':
      return 'Aguardando emissão da carteirinha'
    case 'Active':
      return 'Carteirinha disponível'
    default:
      return state
  }
}

export function SubscriptionConfirmationPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const state = location.state as SubscriptionConfirmationLocationState | undefined

  const [summary, setSummary] = useState<MySubscriptionSummary | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const s = await subscriptionsService.getMySummary()
        if (!cancelled)
          setSummary(s)
      }
      catch (e) {
        if (!cancelled) {
          if (isAxiosError(e) && e.response?.status === 401)
            setLoadError('Sessão expirada. Faça login novamente.')
          else
            setLoadError(e instanceof Error ? e.message : 'Não foi possível carregar o resumo da assinatura.')
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  const checkout = state?.checkout
  const planName = state?.planName ?? summary?.plan?.name
  const billingCycle = state?.billingCycle ?? summary?.plan?.billingCycle
  const amount = checkout?.amount ?? summary?.lastPayment?.amount
  const currency = checkout?.currency ?? summary?.lastPayment?.currency ?? 'BRL'
  const membershipStatus = checkout?.membershipStatus ?? summary?.membershipStatus
  const nextDue = summary?.nextDueDate

  if (!checkout && summary && !summary.hasMembership) {
    return (
      <main style={{ maxWidth: 720, margin: '2rem auto', fontFamily: 'system-ui' }}>
        <h1>Confirmação</h1>
        <p>Não encontramos uma assinatura recente. Se você acabou de contratar, tente voltar pelo fluxo de checkout.</p>
        <p>
          <Link to="/plans">Ver planos</Link>
          {' · '}
          <Link to="/">Início</Link>
        </p>
      </main>
    )
  }

  return (
    <main style={{ maxWidth: 720, margin: '2rem auto', fontFamily: 'system-ui' }}>
      <p>
        <Link to="/plans">← Planos</Link>
        {' · '}
        <Link to="/account">Minha conta</Link>
      </p>
      <h1>Contratação registrada</h1>
      <p style={{ color: '#2a6', fontWeight: 600 }}>Obrigado! Sua solicitação de associação foi recebida.</p>

      {loadError ? <p role="alert" style={{ color: '#721c24' }}>{loadError}</p> : null}

      <section
        style={{
          border: '1px solid #ddd',
          borderRadius: 8,
          padding: '1.25rem',
          background: '#fafafa',
          marginTop: '1rem',
        }}
      >
        <h2 style={{ margin: '0 0 0.75rem', fontSize: '1.1rem' }}>Recibo</h2>
        <ul style={{ margin: 0, paddingLeft: '1.25rem', lineHeight: 1.6 }}>
          <li>
            <strong>Plano:</strong>
            {' '}
            {planName ?? '—'}
            {billingCycle ? ` (${billingCycleLabel(billingCycle)})` : null}
          </li>
          <li>
            <strong>Valor da cobrança inicial:</strong>
            {' '}
            {amount != null ? formatPrice(amount, currency) : '—'}
          </li>
          <li>
            <strong>Status da associação:</strong>
            {' '}
            {membershipStatus ?? '—'}
          </li>
          <li>
            <strong>Próximo vencimento:</strong>
            {' '}
            {nextDue ? formatDate(nextDue) : '—'}
            {' '}
            {membershipStatus === 'PendingPayment' ? '(após confirmação do pagamento)' : null}
          </li>
        </ul>

        {summary?.digitalCard ? (
          <p style={{ margin: '1rem 0 0', fontSize: '0.95rem' }}>
            <strong>Carteirinha digital:</strong>
            {' '}
            {digitalCardStateLabel(summary.digitalCard.state)}
            {summary.digitalCard.message ? ` — ${summary.digitalCard.message}` : null}
          </p>
        ) : null}

        <p style={{ margin: '1.25rem 0 0' }}>
          <Link to="/digital-card" style={{ color: '#1976d2', fontWeight: 600 }}>
            Acessar carteirinha digital
          </Link>
        </p>
      </section>

      {checkout?.pix ? (
        <section style={{ marginTop: '1.5rem' }}>
          <h2 style={{ fontSize: '1rem' }}>Pagamento PIX</h2>
          <p style={{ margin: '0 0 0.35rem', fontSize: '0.85rem', color: '#555' }}>
            Payload do QR (mock / gateway):
          </p>
          <pre
            style={{
              whiteSpace: 'pre-wrap',
              wordBreak: 'break-all',
              background: '#fff',
              border: '1px solid #ccc',
              borderRadius: 6,
              padding: '0.75rem',
              fontSize: '0.8rem',
            }}
          >
            {checkout.pix.qrCodePayload}
          </pre>
          {checkout.pix.copyPasteKey ? (
            <p style={{ margin: '0.75rem 0 0', fontSize: '0.85rem' }}>
              Copia e cola:
              {' '}
              <code>{checkout.pix.copyPasteKey}</code>
            </p>
          ) : null}
        </section>
      ) : null}

      {checkout?.card ? (
        <section style={{ marginTop: '1.5rem' }}>
          <h2 style={{ fontSize: '1rem' }}>Pagamento com cartão</h2>
          <a
            href={checkout.card.checkoutUrl}
            target="_blank"
            rel="noopener noreferrer"
            style={{ color: '#1976d2' }}
          >
            Abrir página de pagamento
          </a>
        </section>
      ) : null}

      <p style={{ margin: '1.5rem 0 0', fontSize: '0.85rem', color: '#666' }}>
        Após o pagamento ser confirmado pelo provedor, sua associação será ativada automaticamente.
      </p>

      <p style={{ marginTop: '1rem' }}>
        <button
          type="button"
          onClick={() => navigate('/')}
          style={{
            padding: '0.5rem 1rem',
            borderRadius: 6,
            border: '1px solid #ccc',
            background: '#fff',
            cursor: 'pointer',
          }}
        >
          Ir ao início
        </button>
      </p>
    </main>
  )
}
