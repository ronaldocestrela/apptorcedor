import { PaymentElement, useElements, useStripe } from '@stripe/react-stripe-js'
import { type FormEvent, useState } from 'react'
import { attachAdminSaasCard } from '../../shared/payments/adminSaasPaymentsApi'

type Props = {
  onSuccess: () => void
  onCancel: () => void
}

export function AdminSaasAddCardForm({ onSuccess, onCancel }: Props) {
  const stripe = useStripe()
  const elements = useElements()
  const [busy, setBusy] = useState(false)
  const [msg, setMsg] = useState<string | null>(null)

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    if (!stripe || !elements) return
    setBusy(true)
    setMsg(null)
    const { error, setupIntent } = await stripe.confirmSetup({
      elements,
      redirect: 'if_required',
    })
    if (error) {
      setMsg(error.message ?? 'Não foi possível confirmar o cartão.')
      setBusy(false)
      return
    }
    const pm = setupIntent?.payment_method
    const id = typeof pm === 'string' ? pm : pm?.id
    if (!id) {
      setMsg('Cartão não confirmado.')
      setBusy(false)
      return
    }
    try {
      await attachAdminSaasCard({ paymentMethodId: id, setAsDefault: true })
      onSuccess()
    } catch (err: unknown) {
      const m = err instanceof Error ? err.message : 'Falha ao registrar o cartão.'
      setMsg(m)
    } finally {
      setBusy(false)
    }
  }

  return (
    <form className="admin-saas-add-card" onSubmit={onSubmit}>
      <PaymentElement />
      {msg ? (
        <p className="billing-page__error" role="alert">
          {msg}
        </p>
      ) : null}
      <div className="admin-saas-add-card__actions">
        <button type="submit" disabled={!stripe || busy}>
          {busy ? 'Salvando…' : 'Salvar cartão'}
        </button>
        <button type="button" className="btn-secondary" disabled={busy} onClick={onCancel}>
          Cancelar
        </button>
      </div>
    </form>
  )
}
