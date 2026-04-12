import { useCallback, useEffect, useState } from 'react'
import { getApiErrorMessage } from '../../shared/auth'
import type { StripeConnectStatusDto } from '../../shared/backoffice/types'
import {
  getTenantStripeConnectStatus,
  startTenantStripeConnectOnboarding,
} from '../../shared/payments/stripeConnectTenantApi'

function isConnectActive(s: StripeConnectStatusDto): boolean {
  return s.chargesEnabled && s.payoutsEnabled
}

export function AdminStripeConnectPage() {
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [status, setStatus] = useState<StripeConnectStatusDto | null>(null)
  const [error, setError] = useState<string | null>(null)

  const loadStatus = useCallback(async (isInitial: boolean) => {
    if (isInitial) {
      setLoading(true)
    } else {
      setBusy(true)
    }
    setError(null)
    try {
      const s = await getTenantStripeConnectStatus()
      setStatus(s)
    } catch (e: unknown) {
      setError(getApiErrorMessage(e, 'Erro ao carregar status.'))
    } finally {
      if (isInitial) {
        setLoading(false)
      } else {
        setBusy(false)
      }
    }
  }, [])

  useEffect(() => {
    void loadStatus(true)
  }, [loadStatus])

  async function onOnboarding() {
    setBusy(true)
    setError(null)
    try {
      const base = `${window.location.origin}/admin/stripe`
      const { url } = await startTenantStripeConnectOnboarding({
        refreshUrl: base,
        returnUrl: base,
      })
      window.open(url, '_blank', 'noopener,noreferrer')
      await loadStatus(false)
    } catch (e: unknown) {
      setError(getApiErrorMessage(e, 'Falha ao gerar link de onboarding.'))
    } finally {
      setBusy(false)
    }
  }

  if (loading) {
    return (
      <section>
        <h1>Stripe Connect</h1>
        <p className="bo-muted">Carregando…</p>
      </section>
    )
  }

  const active = status != null && isConnectActive(status)
  const notConfigured = status != null && !status.isConfigured
  const pending = status != null && status.isConfigured && !active

  return (
    <section>
      <h1>Stripe Connect</h1>
      <p className="bo-muted">
        Configure a conta Stripe do clube para receber pagamentos de sócios (cartão e assinaturas).
      </p>

      {error ? (
        <p className="billing-page__error" role="alert">
          {error}
        </p>
      ) : null}

      {status == null && !error ? (
        <p className="bo-muted">Status não disponível.</p>
      ) : null}

      {notConfigured ? (
        <p role="status">
          Seu clube ainda não configurou uma conta Stripe para receber pagamentos de sócios.
        </p>
      ) : null}

      {pending ? (
        <p role="status">
          Configuração iniciada. Complete o cadastro na Stripe para habilitar cobranças.
        </p>
      ) : null}

      {active ? (
        <p role="status">
          Conta Stripe ativa. Seu clube pode receber pagamentos de sócios.
        </p>
      ) : null}

      {status != null ? (
        <ul className="bo-list bo-divider-top" style={{ marginTop: '1rem' }}>
          <li>Conta: {status.stripeAccountId ?? '—'}</li>
          <li>Cobranças habilitadas: {status.chargesEnabled ? 'sim' : 'não'}</li>
          <li>Repasses habilitados: {status.payoutsEnabled ? 'sim' : 'não'}</li>
          <li>Detalhes enviados: {status.detailsSubmitted ? 'sim' : 'não'}</li>
        </ul>
      ) : null}

      <div className="billing-page__actions" style={{ marginTop: '1rem' }}>
        {!active ? (
          <button type="button" disabled={busy} onClick={() => void onOnboarding()}>
            {notConfigured ? 'Configurar conta Stripe' : 'Retomar configuração'}
          </button>
        ) : null}
        <button
          type="button"
          className="admin-plans__btn-secondary"
          disabled={busy}
          onClick={() => void loadStatus(false)}
        >
          Atualizar status
        </button>
      </div>
    </section>
  )
}
