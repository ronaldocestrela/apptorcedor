import { useEffect, useMemo, useState, type ChangeEvent, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import axios from 'axios'
import { getMyProfile, resolvePublicAssetUrl, upsertMyProfile, uploadProfilePhoto } from '../features/account/accountApi'
import { plansService } from '../features/plans/plansService'
import {
  subscriptionsService,
  type ChangePlanResponse,
  type MySubscriptionSummary,
  type SubscriptionPaymentMethod,
} from '../features/plans/subscriptionsService'
import { useAuth } from '../features/auth/AuthContext'

export function AccountPage() {
  const { user, refreshProfile } = useAuth()
  const [document, setDocument] = useState('')
  const [birthDate, setBirthDate] = useState('')
  const [address, setAddress] = useState('')
  const [photoUrl, setPhotoUrl] = useState<string | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [subscription, setSubscription] = useState<MySubscriptionSummary | null>(null)
  const [subscriptionError, setSubscriptionError] = useState<string | null>(null)
  const [publishedPlans, setPublishedPlans] = useState<Awaited<ReturnType<typeof plansService.listPublished>>['items'] | null>(null)
  const [plansLoadError, setPlansLoadError] = useState<string | null>(null)
  const [selectedPlanId, setSelectedPlanId] = useState('')
  const [changePaymentMethod, setChangePaymentMethod] = useState<SubscriptionPaymentMethod>('Pix')
  const [changeBusy, setChangeBusy] = useState(false)
  const [changeError, setChangeError] = useState<string | null>(null)
  const [changeResult, setChangeResult] = useState<ChangePlanResponse | null>(null)

  const otherPlans = useMemo(() => {
    if (!publishedPlans || !subscription?.plan)
      return []
    return publishedPlans.filter(p => p.planId !== subscription.plan!.planId)
  }, [publishedPlans, subscription?.plan])

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const p = await getMyProfile()
        if (cancelled)
          return
        setDocument(p.document ?? '')
        setBirthDate(p.birthDate ?? '')
        setAddress(p.address ?? '')
        setPhotoUrl(p.photoUrl)
      } catch {
        if (!cancelled)
          setLoadError('Não foi possível carregar o perfil.')
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const s = await subscriptionsService.getMySummary()
        if (!cancelled)
          setSubscription(s)
      }
      catch {
        if (!cancelled)
          setSubscriptionError('Não foi possível carregar dados da assinatura.')
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    if (subscription?.membershipStatus !== 'Ativo' || !subscription.plan) {
      setPublishedPlans(null)
      setPlansLoadError(null)
      setSelectedPlanId('')
      setChangeResult(null)
      return
    }

    let cancelled = false
    setPlansLoadError(null)
    void (async () => {
      try {
        const cat = await plansService.listPublished()
        if (!cancelled)
          setPublishedPlans(cat.items)
      }
      catch {
        if (!cancelled)
          setPlansLoadError('Não foi possível carregar o catálogo de planos.')
      }
    })()
    return () => {
      cancelled = true
    }
  }, [subscription?.membershipStatus, subscription?.plan?.planId])

  async function refreshSubscription() {
    try {
      const s = await subscriptionsService.getMySummary()
      setSubscription(s)
    }
    catch {
      setSubscriptionError('Não foi possível carregar dados da assinatura.')
    }
  }

  async function onConfirmPlanChange() {
    if (!selectedPlanId)
      return
    setChangeError(null)
    setChangeResult(null)
    setChangeBusy(true)
    try {
      const r = await subscriptionsService.changePlan(selectedPlanId, changePaymentMethod)
      setChangeResult(r)
      await refreshSubscription()
    }
    catch (err) {
      if (axios.isAxiosError(err)) {
        const code = (err.response?.data as { error?: string } | undefined)?.error
        const map: Record<string, string> = {
          membership_not_found: 'Assinatura não encontrada.',
          membership_not_active: 'Só é possível trocar com assinatura ativa.',
          missing_billing_context: 'Não há dados de ciclo de cobrança para calcular o proporcional.',
          plan_not_available: 'Plano indisponível.',
          same_plan: 'Selecione um plano diferente do atual.',
        }
        setChangeError(code ? (map[code] ?? 'Não foi possível concluir a troca.') : 'Não foi possível concluir a troca.')
      }
      else {
        setChangeError('Não foi possível concluir a troca.')
      }
    }
    finally {
      setChangeBusy(false)
    }
  }

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setSaveError(null)
    setBusy(true)
    try {
      await upsertMyProfile({
        document: document.trim() || null,
        birthDate: birthDate.trim() || null,
        address: address.trim() || null,
        photoUrl: photoUrl ?? null,
      })
      await refreshProfile()
    } catch {
      setSaveError('Falha ao salvar.')
    } finally {
      setBusy(false)
    }
  }

  async function onPhoto(ev: ChangeEvent<HTMLInputElement>) {
    const file = ev.target.files?.[0]
    if (!file)
      return
    setSaveError(null)
    setBusy(true)
    try {
      const url = await uploadProfilePhoto(file)
      setPhotoUrl(url)
      await upsertMyProfile({ photoUrl: url })
      await refreshProfile()
    } catch {
      setSaveError('Falha no envio da foto (tipo ou tamanho).')
    } finally {
      setBusy(false)
    }
  }

  return (
    <main style={{ maxWidth: 480, margin: '2rem auto', fontFamily: 'system-ui' }}>
      <h1>Minha conta</h1>
      <p>
        <strong>{user?.name}</strong> ({user?.email})
      </p>
      {user?.requiresProfileCompletion ? (
        <p style={{ color: '#856404', background: '#fff3cd', padding: 8 }}>
          Complete seu perfil (documento obrigatório para seguir).
        </p>
      ) : null}
      {subscriptionError ? <p style={{ color: '#721c24', fontSize: '0.9rem' }}>{subscriptionError}</p> : null}
      {subscription?.hasMembership ? (
        <section
          style={{
            border: '1px solid #ddd',
            borderRadius: 8,
            padding: '1rem',
            marginBottom: '1rem',
            background: '#f9f9f9',
          }}
        >
          <h2 style={{ margin: '0 0 0.5rem', fontSize: '1.05rem' }}>Assinatura</h2>
          <p style={{ margin: '0.25rem 0' }}>
            <strong>Status:</strong>
            {' '}
            {subscription.membershipStatus ?? '—'}
          </p>
          <p style={{ margin: '0.25rem 0' }}>
            <strong>Próximo vencimento:</strong>
            {' '}
            {subscription.nextDueDate
              ? new Date(subscription.nextDueDate).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
              : '—'}
          </p>
          {subscription.plan ? (
            <p style={{ margin: '0.25rem 0', fontSize: '0.9rem', color: '#444' }}>
              Plano:
              {' '}
              {subscription.plan.name}
            </p>
          ) : null}
          <p style={{ margin: '0.75rem 0 0' }}>
            <Link to="/digital-card">Carteirinha digital</Link>
            {' · '}
            <Link to="/plans">Planos</Link>
          </p>
          {subscription.membershipStatus === 'Ativo' && subscription.plan ? (
            <div style={{ marginTop: '1rem', paddingTop: '1rem', borderTop: '1px solid #ddd' }}>
              <h3 style={{ margin: '0 0 0.5rem', fontSize: '1rem' }}>Trocar plano</h3>
              {plansLoadError ? <p style={{ color: '#721c24', fontSize: '0.9rem' }}>{plansLoadError}</p> : null}
              {!plansLoadError && publishedPlans === null ? (
                <p style={{ fontSize: '0.9rem', color: '#555' }}>Carregando planos…</p>
              ) : null}
              {!plansLoadError && publishedPlans && otherPlans.length === 0 ? (
                <p style={{ fontSize: '0.9rem', color: '#555' }}>Não há outros planos publicados para troca.</p>
              ) : null}
              {!plansLoadError && otherPlans.length > 0 ? (
                <>
                  <label style={{ display: 'block', marginBottom: 8, fontSize: '0.9rem' }}>
                    Outro plano
                    <select
                      value={selectedPlanId}
                      onChange={(ev) => {
                        setSelectedPlanId(ev.target.value)
                        setChangeResult(null)
                      }}
                      style={{ display: 'block', width: '100%', marginTop: 4 }}
                    >
                      <option value="">Selecione…</option>
                      {otherPlans.map(p => (
                        <option key={p.planId} value={p.planId}>
                          {p.name}
                          {' '}
                          —
                          {' '}
                          {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(p.price)}
                          {' '}
                          (
                          {p.billingCycle}
                          )
                        </option>
                      ))}
                    </select>
                  </label>
                  <fieldset style={{ border: 'none', padding: 0, margin: '0 0 8px' }}>
                    <legend style={{ fontSize: '0.85rem', marginBottom: 4 }}>Pagamento do proporcional</legend>
                    <label style={{ marginRight: 12, fontSize: '0.9rem' }}>
                      <input
                        type="radio"
                        name="changePlanPm"
                        checked={changePaymentMethod === 'Pix'}
                        onChange={() => setChangePaymentMethod('Pix')}
                      />
                      {' '}
                      Pix
                    </label>
                    <label style={{ fontSize: '0.9rem' }}>
                      <input
                        type="radio"
                        name="changePlanPm"
                        checked={changePaymentMethod === 'Card'}
                        onChange={() => setChangePaymentMethod('Card')}
                      />
                      {' '}
                      Cartão
                    </label>
                  </fieldset>
                  <button
                    type="button"
                    disabled={!selectedPlanId || changeBusy}
                    onClick={() => void onConfirmPlanChange()}
                  >
                    {changeBusy ? 'Processando…' : 'Confirmar troca'}
                  </button>
                </>
              ) : null}
              {changeError ? <p role="alert" style={{ color: 'crimson', fontSize: '0.9rem', marginTop: 8 }}>{changeError}</p> : null}
              {changeResult ? (
                <div style={{ marginTop: 12, fontSize: '0.9rem', background: '#fff', padding: 8, borderRadius: 6 }}>
                  <p style={{ margin: '0 0 0.5rem' }}>
                    <strong>Troca registrada.</strong>
                    {' '}
                    Proporcional:
                    {' '}
                    {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: changeResult.currency || 'BRL' }).format(changeResult.prorationAmount)}
                  </p>
                  {changeResult.prorationAmount > 0 && changeResult.pix ? (
                    <div>
                      <p style={{ margin: '0.25rem 0' }}>PIX — copia e cola:</p>
                      <pre style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-all', fontSize: '0.75rem' }}>
                        {changeResult.pix.copyPasteKey ?? changeResult.pix.qrCodePayload}
                      </pre>
                    </div>
                  ) : null}
                  {changeResult.prorationAmount > 0 && changeResult.card ? (
                    <p style={{ margin: '0.5rem 0 0' }}>
                      <a href={changeResult.card.checkoutUrl} target="_blank" rel="noreferrer">
                        Abrir checkout do cartão
                      </a>
                    </p>
                  ) : null}
                  {changeResult.prorationAmount === 0 ? (
                    <p style={{ margin: 0, color: '#555' }}>Sem cobrança proporcional. Seu plano já foi atualizado.</p>
                  ) : null}
                </div>
              ) : null}
            </div>
          ) : null}
        </section>
      ) : !subscriptionError && subscription && !subscription.hasMembership ? (
        <p style={{ fontSize: '0.95rem', color: '#555' }}>
          Você ainda não possui assinatura de sócio.
          {' '}
          <Link to="/plans">Ver planos</Link>
        </p>
      ) : null}
      {loadError ? <p role="alert" style={{ color: 'crimson' }}>{loadError}</p> : null}
      {photoUrl ? (
        <p>
          <img
            src={resolvePublicAssetUrl(photoUrl)}
            alt="Foto"
            style={{ maxWidth: 160, borderRadius: 8 }}
          />
        </p>
      ) : null}
      <form onSubmit={onSubmit}>
        <label style={{ display: 'block', marginBottom: 8 }}>
          Documento (CPF ou equivalente)
          <input
            value={document}
            onChange={(ev) => setDocument(ev.target.value)}
            style={{ display: 'block', width: '100%', marginTop: 4 }}
          />
        </label>
        <label style={{ display: 'block', marginBottom: 8 }}>
          Data de nascimento
          <input
            type="date"
            value={birthDate}
            onChange={(ev) => setBirthDate(ev.target.value)}
            style={{ display: 'block', width: '100%', marginTop: 4 }}
          />
        </label>
        <label style={{ display: 'block', marginBottom: 8 }}>
          Endereço
          <textarea
            value={address}
            onChange={(ev) => setAddress(ev.target.value)}
            rows={3}
            style={{ display: 'block', width: '100%', marginTop: 4 }}
          />
        </label>
        <label style={{ display: 'block', marginBottom: 16 }}>
          Foto do perfil
          <input type="file" accept="image/jpeg,image/png,image/webp" onChange={(ev) => void onPhoto(ev)} disabled={busy} />
        </label>
        {saveError ? <p role="alert" style={{ color: 'crimson' }}>{saveError}</p> : null}
        <button type="submit" disabled={busy}>
          {busy ? 'Salvando...' : 'Salvar perfil'}
        </button>
      </form>
      <p style={{ marginTop: 24 }}>
        <Link to="/">Voltar</Link>
      </p>
    </main>
  )
}
