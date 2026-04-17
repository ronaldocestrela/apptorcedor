import { useEffect, useState, type ReactNode } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { ArrowLeft, Settings } from 'lucide-react'
import { isAxiosError } from 'axios'
import {
  subscriptionsService,
  type MySubscriptionSummary,
  type TorcedorSubscriptionCheckoutResponse,
} from '../features/plans/subscriptionsService'
import { useAuth } from '../features/auth/AuthContext'
import './AppShell.css'

export type SubscriptionConfirmationLocationState = {
  checkout: TorcedorSubscriptionCheckoutResponse
  planName: string
  billingCycle: string
}

function billingCyclePeriodShort(cycle: string | undefined): string {
  if (!cycle)
    return '—'
  switch (cycle) {
    case 'Monthly':
      return 'mês'
    case 'Yearly':
      return 'ano'
    case 'Quarterly':
      return 'trimestre'
    default:
      return cycle
  }
}

function formatPrice(value: number, currency: string): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: currency === 'BRL' ? 'BRL' : 'BRL' })
}

function formatDueShort(iso: string | null | undefined): string {
  if (!iso)
    return '—'
  const d = new Date(iso)
  if (Number.isNaN(d.getTime()))
    return '—'
  return d.toLocaleString('pt-BR', { day: '2-digit', month: '2-digit' })
}

function ConfirmationShell({ children }: { children: ReactNode }) {
  return (
    <div className="plans-root">
      <header className="subpage-header">
        <Link to="/plans" className="subpage-header__back" aria-label="Voltar">
          <ArrowLeft size={18} />
        </Link>
        <h1 className="subpage-header__title plans-page__header-title">Planos</h1>
        <Link to="/account" className="plans-page__settings-btn" aria-label="Configurações">
          <Settings size={20} stroke="currentColor" />
        </Link>
      </header>
      {children}
    </div>
  )
}

export function SubscriptionConfirmationPage() {
  const { user } = useAuth()
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
  const holderName = user?.name?.trim() ? user.name : '—'
  const showPendingPayment = membershipStatus === 'PendingPayment'
  const valueLine
    = amount != null
      ? `${formatPrice(amount, currency)} / ${billingCyclePeriodShort(billingCycle)}`
      : '—'

  if (!checkout && summary && !summary.hasMembership) {
    return (
      <ConfirmationShell>
        <main className="subpage-content sub-confirm-empty">
          <h1 className="sub-confirm-empty__title">Confirmação</h1>
          <p className="sub-confirm-empty__text">
            Não encontramos uma assinatura recente. Se você acabou de contratar, tente voltar pelo fluxo de checkout.
          </p>
          <p className="sub-confirm-empty__links">
            <Link to="/plans">Ver planos</Link>
            <span className="app-muted" aria-hidden="true">·</span>
            <Link to="/">Início</Link>
          </p>
        </main>
      </ConfirmationShell>
    )
  }

  return (
    <ConfirmationShell>
      <main className="subpage-content sub-confirm">
        <p className="sub-confirm__eyebrow">Você acabou de se tornar</p>
        <span className="plans-page__badge sub-confirm__plan-pill">{planName ?? '—'}</span>

        {loadError ? <p role="alert" className="sub-confirm__alert">{loadError}</p> : null}

        <section className="sub-confirm__card" aria-labelledby="sub-confirm-receipt-title">
          <h2 id="sub-confirm-receipt-title" className="sub-confirm__card-title">Recibo</h2>
          <p className="sub-confirm__holder">{holderName}</p>
          <ul className="sub-confirm__rows">
            <li>
              Plano:
              {' '}
              <strong>{planName ?? '—'}</strong>
            </li>
            <li>
              Valor:
              {' '}
              <strong>{valueLine}</strong>
            </li>
            <li>
              Status da Associação:
              {' '}
              <strong>{membershipStatus ?? '—'}</strong>
            </li>
            <li>
              Data de vencimento:
              {' '}
              <strong>
                {formatDueShort(nextDue)}
                {showPendingPayment ? ' (após confirmação do pagamento)' : null}
              </strong>
            </li>
          </ul>
          <Link to="/digital-card" className="sub-confirm__cta">
            CARTEIRINHA
          </Link>
        </section>

        {showPendingPayment && checkout?.pix ? (
          <section className="sub-confirm__pending" aria-labelledby="sub-confirm-pix-title">
            <h3 id="sub-confirm-pix-title" className="sub-confirm__pending-title">Pagamento PIX</h3>
            <p className="sub-confirm__pending-caption">Payload do QR (mock / gateway):</p>
            <pre className="sub-confirm__pending-pre">{checkout.pix.qrCodePayload}</pre>
            {checkout.pix.copyPasteKey ? (
              <p className="sub-confirm__pending-copy">
                Copia e cola:
                {' '}
                <code>{checkout.pix.copyPasteKey}</code>
              </p>
            ) : null}
          </section>
        ) : null}

        {showPendingPayment && checkout?.card ? (
          <section className="sub-confirm__pending" aria-labelledby="sub-confirm-card-title">
            <h3 id="sub-confirm-card-title" className="sub-confirm__pending-title">Pagamento com cartão</h3>
            <a
              href={checkout.card.checkoutUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="sub-confirm__pending-link"
            >
              Abrir página de pagamento
            </a>
          </section>
        ) : null}

        {showPendingPayment ? (
          <p className="sub-confirm__hint">
            Após o pagamento ser confirmado pelo provedor, sua associação será ativada automaticamente.
          </p>
        ) : null}

        <p className="sub-confirm__return">
          Retornar a
          {' '}
          <Link to="/">página inicial</Link>
          .
        </p>
      </main>
    </ConfirmationShell>
  )
}
