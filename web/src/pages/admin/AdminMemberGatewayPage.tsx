import { useCallback, useEffect, useState } from 'react'
import { getApiErrorMessage } from '../../shared/auth'
import {
  configureTenantStripeDirect,
  getTenantMemberGatewayStatus,
} from '../../shared/payments/memberGatewayTenantApi'

export function AdminMemberGatewayPage() {
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [status, setStatus] = useState<Awaited<ReturnType<typeof getTenantMemberGatewayStatus>> | null>(
    null,
  )
  const [error, setError] = useState<string | null>(null)
  const [secretKey, setSecretKey] = useState('')
  const [publishableKey, setPublishableKey] = useState('')
  const [webhookSecret, setWebhookSecret] = useState('')

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const s = await getTenantMemberGatewayStatus()
      setStatus(s)
    } catch (e: unknown) {
      setError(getApiErrorMessage(e, 'Erro ao carregar status do gateway.'))
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  async function onSave(e: React.FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    if (!secretKey.trim()) {
      setError('Informe a secret key (sk_…). A API exige a chave em todo salvamento.')
      setBusy(false)
      return
    }
    try {
      await configureTenantStripeDirect({
        secretKey: secretKey.trim(),
        publishableKey: publishableKey.trim() || null,
        webhookSecret: webhookSecret.trim() || null,
      })
      setSecretKey('')
      setWebhookSecret('')
      await load()
    } catch (err: unknown) {
      setError(getApiErrorMessage(err, 'Não foi possível salvar as credenciais.'))
    } finally {
      setBusy(false)
    }
  }

  if (loading) {
    return (
      <section>
        <h1>Gateway de pagamentos (sócios)</h1>
        <p className="bo-muted">Carregando…</p>
      </section>
    )
  }

  const provider = status?.selectedProvider ?? 'None'
  const stripeDirect = provider === 'StripeDirect'

  return (
    <section>
      <h1>Gateway de pagamentos (sócios)</h1>
      <p className="bo-muted">
        Credenciais da <strong>conta Stripe do clube</strong> (modo direto, sem Stripe Connect). O operador da
        plataforma deve selecionar o provedor no backoffice antes de você informar as chaves.
      </p>

      {error ? (
        <p className="billing-page__error" role="alert">
          {error}
        </p>
      ) : null}

      {status ? (
        <ul className="bo-list bo-divider-top" style={{ marginTop: '1rem' }}>
          <li>Provedor selecionado: {provider}</li>
          <li>Status: {status.status}</li>
          <li>Chave publicável (máscara): {status.publishableKeyHint ?? '—'}</li>
          <li>Segredo de webhook configurado: {status.webhookSecretConfigured ? 'sim' : 'não'}</li>
        </ul>
      ) : null}

      {!stripeDirect ? (
        <p className="billing-page__error" role="status" style={{ marginTop: '1rem' }}>
          O provedor <strong>StripeDirect</strong> ainda não foi atribuído a este clube no backoffice da
          plataforma. Peça ao operador para definir o gateway antes de salvar chaves aqui.
        </p>
      ) : null}

      <h2 style={{ marginTop: '1.5rem' }}>Credenciais Stripe (conta do clube)</h2>
      <p className="bo-muted">
        Use a secret key (sk_…) da conta Stripe do clube, a chave publicável (pk_…) e o signing secret do
        webhook thin (whsec_…) configurado na Stripe apontando para{' '}
        <code>
          {`${typeof window !== 'undefined' ? window.location.origin : ''}/api/webhooks/stripe/member/<tenantId>`}
        </code>{' '}
        (o <code>tenantId</code> é o GUID do clube no master).
      </p>

      <form className="bo-form-grid" onSubmit={onSave} style={{ marginTop: '1rem' }}>
        <label className="billing-page__field">
          Secret key (sk_…)
          <input
            className="auth-field__input"
            type="password"
            autoComplete="off"
            value={secretKey}
            onChange={(e) => setSecretKey(e.target.value)}
            disabled={busy || !stripeDirect}
            placeholder="Obrigatório a cada salvamento (a API não atualiza credenciais sem nova secret)"
          />
        </label>
        <label className="billing-page__field">
          Chave publicável (pk_…)
          <input
            className="auth-field__input"
            type="text"
            autoComplete="off"
            value={publishableKey}
            onChange={(e) => setPublishableKey(e.target.value)}
            disabled={busy || !stripeDirect}
          />
        </label>
        <label className="billing-page__field">
          Webhook signing secret (whsec_…)
          <input
            className="auth-field__input"
            type="password"
            autoComplete="off"
            value={webhookSecret}
            onChange={(e) => setWebhookSecret(e.target.value)}
            disabled={busy || !stripeDirect}
            placeholder="Obrigatório para validar webhooks thin"
          />
        </label>
        <p>
          <button type="submit" disabled={busy || !stripeDirect}>
            Salvar credenciais
          </button>
          <button type="button" className="admin-plans__btn-secondary" disabled={busy} onClick={() => void load()}>
            Recarregar status
          </button>
        </p>
      </form>
    </section>
  )
}
